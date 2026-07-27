using System.IO;
using System.Text;

namespace GC.AuctionFlow.Recorder;

public sealed class RecoveryFinding
{
    public RecoveryFinding(
        RecoveryClassification classification,
        string path,
        string detail,
        string? quarantinePath = null)
    {
        Classification = classification;
        Path = path;
        Detail = detail;
        QuarantinePath = quarantinePath;
    }

    public RecoveryClassification Classification { get; }
    public string Path { get; }
    public string Detail { get; }
    public string? QuarantinePath { get; }
}

public sealed class RecoveryScanResult
{
    public RecoveryScanResult(
        IReadOnlyList<RecoveryFinding> findings,
        IReadOnlyList<CompletedSegmentInfo> trustedSegments)
    {
        Findings = findings;
        TrustedSegments = trustedSegments;
    }

    public IReadOnlyList<RecoveryFinding> Findings { get; }
    public IReadOnlyList<CompletedSegmentInfo> TrustedSegments { get; }
}

/// <summary>
/// Read-only toward original evidence. Never silently deletes or rewrites segment bytes.
/// </summary>
public static class RecoveryScanner
{
    public static RecoveryScanResult ScanSessionDirectory(
        string sessionDirectory,
        RecorderConfig config,
        bool copyQuarantine = true)
    {
        var findings = new List<RecoveryFinding>();
        var trusted = new List<CompletedSegmentInfo>();
        var segmentsDir = Path.Combine(sessionDirectory, RecorderStoragePaths.SegmentsDirectoryName);
        var recoveryDir = Path.Combine(sessionDirectory, RecorderStoragePaths.RecoveryDirectoryName);
        try
        {
            Directory.CreateDirectory(recoveryDir);
        }
        catch (Exception ex)
        {
            findings.Add(new RecoveryFinding(
                RecoveryClassification.IncompleteTemporary,
                recoveryDir,
                "RecoveryDirectoryUnavailable; QuarantineCopyFailed: " + ex.GetType().Name));
        }

        if (!Directory.Exists(segmentsDir))
            return new RecoveryScanResult(findings, trusted);

        var segFiles = Directory.GetFiles(segmentsDir, "*.seg");
        var tmpFiles = Directory.GetFiles(segmentsDir, "*.seg.tmp");
        var hashFiles = Directory.GetFiles(segmentsDir, "*.seg.sha256");

        foreach (var tmp in tmpFiles)
        {
            var q = MaybeQuarantine(tmp, recoveryDir, copyQuarantine, out var qFail);
            var detail = "Orphan temporary segment";
            if (qFail) detail += "; QuarantineCopyFailed";
            findings.Add(new RecoveryFinding(RecoveryClassification.IncompleteTemporary, tmp, detail, q));
        }

        var hashByStem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in hashFiles)
        {
            var fileName = Path.GetFileName(h);
            var stem = fileName.EndsWith(".seg.sha256", StringComparison.OrdinalIgnoreCase)
                ? fileName[..^".seg.sha256".Length]
                : fileName;
            hashByStem[stem] = h;
        }

        var seenIds = new HashSet<Guid>();
        foreach (var seg in segFiles)
        {
            var name = Path.GetFileName(seg);
            var stem = name.EndsWith(".seg", StringComparison.OrdinalIgnoreCase)
                ? name[..^4]
                : name;

            if (!hashByStem.Remove(stem, out var hashPath))
            {
                var q = MaybeQuarantine(seg, recoveryDir, copyQuarantine, out var qFail);
                var detail = "Completed segment missing companion hash";
                if (qFail) detail += "; QuarantineCopyFailed";
                findings.Add(new RecoveryFinding(RecoveryClassification.HashMissing, seg, detail, q));
                continue;
            }

            if (!SegmentWriter.TryReadSha256File(hashPath, out var expectedSha))
            {
                findings.Add(new RecoveryFinding(RecoveryClassification.HashMissing, hashPath, "Unreadable hash file"));
                continue;
            }

            string actualSha;
            try
            {
                actualSha = SegmentWriter.ComputeSha256Hex(seg);
            }
            catch (Exception ex)
            {
                findings.Add(new RecoveryFinding(RecoveryClassification.HashMismatch, seg, "Hash compute failed: " + ex.Message));
                continue;
            }

            if (!string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase))
            {
                var q = MaybeQuarantine(seg, recoveryDir, copyQuarantine, out var qFail);
                var detail = $"Expected {expectedSha}, actual {actualSha}";
                if (qFail) detail += "; QuarantineCopyFailed";
                findings.Add(new RecoveryFinding(RecoveryClassification.HashMismatch, seg, detail, q));
                continue;
            }

            var validate = ValidateSegmentFile(seg, config, out var info, out var classification, out var detail2);
            if (!validate || info is null)
            {
                var q = MaybeQuarantine(seg, recoveryDir, copyQuarantine, out var qFail);
                var detail = detail2 ?? "Invalid segment";
                if (qFail) detail += "; QuarantineCopyFailed";
                findings.Add(new RecoveryFinding(classification, seg, detail, q));
                continue;
            }

            if (!seenIds.Add(info.SegmentId))
            {
                var q = MaybeQuarantine(seg, recoveryDir, copyQuarantine, out var qFail);
                var detail = "Duplicate segment identity";
                if (qFail) detail += "; QuarantineCopyFailed";
                findings.Add(new RecoveryFinding(RecoveryClassification.DuplicateSegmentIdentity, seg, detail, q));
                continue;
            }

            trusted.Add(info);
            findings.Add(new RecoveryFinding(RecoveryClassification.TrustedComplete, seg, "Trusted"));
        }

        foreach (var orphanHash in hashByStem.Values)
        {
            var q = MaybeQuarantine(orphanHash, recoveryDir, copyQuarantine, out var qFail);
            var detail = "Hash without segment";
            if (qFail) detail += "; QuarantineCopyFailed";
            findings.Add(new RecoveryFinding(RecoveryClassification.OrphanHash, orphanHash, detail, q));
        }

        var manifestPath = Path.Combine(sessionDirectory, RecorderStoragePaths.ManifestFileName);
        if (File.Exists(manifestPath))
        {
            try
            {
                var bytes = File.ReadAllBytes(manifestPath);
                var man = RecorderJson.DeserializeManifest(bytes);
                if (man is not null)
                {
                    var listed = man.Segments.Select(s => s.SegmentId).ToHashSet();
                    foreach (var t in trusted)
                    {
                        if (!listed.Contains(t.SegmentId))
                        {
                            findings.Add(new RecoveryFinding(
                                RecoveryClassification.ManifestLag,
                                manifestPath,
                                $"Trusted segment {t.SegmentId:N} missing from manifest"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                findings.Add(new RecoveryFinding(RecoveryClassification.PayloadDecodeFailure, manifestPath, ex.Message));
            }
        }
        else if (trusted.Count > 0)
        {
            findings.Add(new RecoveryFinding(RecoveryClassification.ManifestLag, sessionDirectory, "Manifest missing while trusted segments exist"));
        }

        return new RecoveryScanResult(findings, trusted.OrderBy(t => t.SegmentOrdinal).ToList());
    }

    public static SessionManifestRecord RebuildManifestFromTrusted(
        Guid sessionId,
        Guid processInstanceId,
        DateTime startUtc,
        DateTime? stopUtc,
        IReadOnlyList<CompletedSegmentInfo> trusted,
        RecorderCountersSnapshot counters,
        ObservedInstrumentIdentity? lastInstrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance)
    {
        var epochs = trusted.Select(t => t.ContractEpoch).Distinct().OrderBy(x => x).ToList();
        var segments = trusted.Select(t => new ManifestSegmentRecord(
            t.SegmentId,
            t.SegmentOrdinal,
            t.FileName,
            t.Sha256Hex,
            t.RecordCount,
            t.MarketEventRecordCount,
            t.InvocationResultRecordCount,
            t.LifecycleIntegrityRecordCount,
            t.ByteLength,
            t.FirstWriterSequence,
            t.LastWriterSequence,
            t.ContractEpoch,
            t.CategoryCountsKnown)).ToList();

        return new SessionManifestRecord(
            RawEventRecorderVersions.ManifestGeneration,
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            RawEventRecorderVersions.RawEventContainerVersion,
            sessionId,
            processInstanceId,
            startUtc,
            stopUtc,
            epochs,
            declaredMode,
            modeProvenance,
            declaredProvider,
            providerProvenance,
            lastInstrument,
            new[] { "Trade" },
            new[]
            {
                new DisabledStreamRecord("Mbo", MboOperationalLock.MboOperationalBlockReason)
            },
            MboOperationalLock.MboSchemaSupported,
            MboOperationalLock.MboRecordingEnabledDefault,
            MboOperationalLock.MboIsolationRequirement.ToString(),
            MboOperationalLock.MboOperationalBlockReason,
            segments,
            trusted.Count == 0 ? null : trusted.Min(t => t.FirstWriterSequence),
            trusted.Count == 0 ? null : trusted.Max(t => t.LastWriterSequence),
            counters.RecordsWritten,
            counters.BytesWritten,
            counters,
            abnormalTermination: false,
            abnormalTerminationReason: null,
            new[]
            {
                "LOCAL_CAPTURE_NOT_EXCHANGE_COMPLETENESS",
                "SEGMENTS_AUTHORITATIVE_MANIFEST_RECOVERABLE_INDEX"
            },
            new CapabilityClaimsForcedFalse(),
            RawEventRecorderVersions.ContinuityDisclaimer);
    }

    private static string? MaybeQuarantine(string path, string recoveryDir, bool copy, out bool quarantineCopyFailed)
    {
        quarantineCopyFailed = false;
        if (!copy || !File.Exists(path)) return null;
        var dest = Path.Combine(recoveryDir, Path.GetFileName(path) + ".quarantine");
        try
        {
            File.Copy(path, dest, overwrite: true);
            return dest;
        }
        catch
        {
            quarantineCopyFailed = true;
            return null;
        }
    }

    public static bool ValidateSegmentFile(
        string path,
        RecorderConfig config,
        out CompletedSegmentInfo? info,
        out RecoveryClassification classification,
        out string? detail)
    {
        info = null;
        classification = RecoveryClassification.TrustedComplete;
        detail = null;

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            classification = RecoveryClassification.InvalidContainerHeader;
            detail = ex.Message;
            return false;
        }

        if (!ContainerFormat.TryReadContainerHeader(bytes, out _, out _, out var headerError))
        {
            classification = RecoveryClassification.InvalidContainerHeader;
            detail = headerError;
            return false;
        }

        var offset = ContainerFormat.ContainerHeaderSize;
        SegmentHeaderRecord? header = null;
        SegmentFooterRecord? footer = null;
        long rawEventCount = 0;
        long marketCount = 0;
        long invResultCount = 0;
        long lifecycleCount = 0;
        long? prevSeq = null;
        var hadSequenceDiscontinuity = false;
        var hadPayloadDecodeFailure = false;
        string? payloadDecodeDetail = null;

        while (offset < bytes.Length)
        {
            var status = FrameCodec.TryDecodeFrame(
                bytes.AsSpan(offset),
                config.MaxFramePayloadBytes,
                out var frame,
                out var consumed,
                out var frameDetail);

            if (status == FrameDecodeStatus.NeedMoreData)
            {
                classification = RecoveryClassification.MissingFooter;
                detail = "TruncatedFrame";
                return false;
            }

            if (status == FrameDecodeStatus.InvalidFrameMagic)
            {
                classification = RecoveryClassification.InvalidFrameMagic;
                detail = frameDetail;
                return false;
            }

            if (status == FrameDecodeStatus.InvalidFrameLength)
            {
                classification = RecoveryClassification.InvalidFrameLength;
                detail = frameDetail;
                return false;
            }

            if (status == FrameDecodeStatus.OversizedFrame)
            {
                classification = RecoveryClassification.OversizedFrame;
                detail = frameDetail;
                return false;
            }

            if (status == FrameDecodeStatus.FrameCrcMismatch)
            {
                classification = RecoveryClassification.FrameCrcMismatch;
                detail = frameDetail;
                return false;
            }

            if (status != FrameDecodeStatus.Ok)
            {
                classification = RecoveryClassification.InvalidFrameLength;
                detail = frameDetail ?? status.ToString();
                return false;
            }

            offset += consumed;

            switch (frame.FrameType)
            {
                case RecorderFrameType.SegmentHeader:
                    try
                    {
                        header = RecorderJson.DeserializeHeader(frame.PayloadUtf8);
                        if (header is null)
                        {
                            classification = RecoveryClassification.PayloadDecodeFailure;
                            detail = "HeaderDecodeNull";
                            return false;
                        }
                    }
                    catch
                    {
                        classification = RecoveryClassification.PayloadDecodeFailure;
                        detail = "HeaderDecodeFailure";
                        return false;
                    }

                    break;

                case RecorderFrameType.RawEvent:
                    rawEventCount++;
                    try
                    {
                        var env = RecorderJson.DeserializeEnvelope(frame.PayloadUtf8);
                        if (env is null)
                        {
                            hadPayloadDecodeFailure = true;
                            payloadDecodeDetail = "RawEventDecodeNull";
                        }
                        else
                        {
                            switch (RawEventRecordCategoryClassifier.Classify(env.PayloadDiscriminator))
                            {
                                case RawEventRecordCategory.MarketEvent:
                                    marketCount++;
                                    break;
                                case RawEventRecordCategory.CallbackInvocationResult:
                                    invResultCount++;
                                    break;
                                case RawEventRecordCategory.LifecycleIntegrity:
                                    lifecycleCount++;
                                    break;
                            }

                            if (prevSeq is not null && env.RecorderGlobalLocalSequence != prevSeq.Value + 1)
                                hadSequenceDiscontinuity = true;
                            prevSeq = env.RecorderGlobalLocalSequence;
                        }
                    }
                    catch
                    {
                        // CRC/length valid but malformed JSON — continue to next frame; not trusted.
                        hadPayloadDecodeFailure = true;
                        payloadDecodeDetail = "RawEventDecodeFailure";
                    }

                    break;

                case RecorderFrameType.SegmentFooter:
                    try
                    {
                        footer = RecorderJson.DeserializeFooter(frame.PayloadUtf8);
                        if (footer is null)
                        {
                            classification = RecoveryClassification.PayloadDecodeFailure;
                            detail = "FooterDecodeNull";
                            return false;
                        }
                    }
                    catch
                    {
                        classification = RecoveryClassification.PayloadDecodeFailure;
                        detail = "FooterDecodeFailure";
                        return false;
                    }

                    break;
            }
        }

        if (header is null)
        {
            classification = RecoveryClassification.InvalidContainerHeader;
            detail = "MissingHeader";
            return false;
        }

        if (footer is null)
        {
            classification = RecoveryClassification.MissingFooter;
            detail = "MissingFooter";
            return false;
        }

        if (hadSequenceDiscontinuity)
        {
            classification = RecoveryClassification.SequenceDiscontinuity;
            detail = "Writer sequence discontinuity inside segment";
            return false;
        }

        if (hadPayloadDecodeFailure)
        {
            classification = RecoveryClassification.PayloadDecodeFailure;
            detail = payloadDecodeDetail;
            return false;
        }

        if (footer.RawEventRecordCount != rawEventCount)
        {
            classification = RecoveryClassification.SequenceDiscontinuity;
            detail = $"Footer RawEventRecordCount {footer.RawEventRecordCount} != scanned {rawEventCount}";
            return false;
        }

        var requireCategories = RecorderSchemaCompatibility.RequiresCategoryCounts(header.RecorderSchemaVersion);
        if (requireCategories)
        {
            if (footer.RawEventRecordCount
                != footer.MarketEventRecordCount + footer.InvocationResultRecordCount + footer.LifecycleIntegrityRecordCount)
            {
                classification = RecoveryClassification.ReconciliationMismatch;
                detail = "Footer category sum != RawEventRecordCount";
                return false;
            }

            if (footer.MarketEventRecordCount != marketCount
                || footer.InvocationResultRecordCount != invResultCount
                || footer.LifecycleIntegrityRecordCount != lifecycleCount)
            {
                classification = RecoveryClassification.ReconciliationMismatch;
                detail =
                    $"Footer categories M={footer.MarketEventRecordCount}/I={footer.InvocationResultRecordCount}/L={footer.LifecycleIntegrityRecordCount} != scanned M={marketCount}/I={invResultCount}/L={lifecycleCount}";
                return false;
            }
        }

        if (!SegmentWriter.TryReadSha256File(
                Path.Combine(Path.GetDirectoryName(path)!, RecorderStoragePaths.SegmentHashFileName(header.SegmentId)),
                out var sha))
        {
            sha = SegmentWriter.ComputeSha256Hex(path);
        }

        info = new CompletedSegmentInfo(
            header.SegmentId,
            header.SegmentOrdinal,
            Path.GetFileName(path),
            path,
            sha,
            rawEventCount,
            requireCategories ? marketCount : 0,
            requireCategories ? invResultCount : 0,
            requireCategories ? lifecycleCount : 0,
            bytes.LongLength,
            footer.FirstWriterSequence,
            footer.LastWriterSequence,
            header.ContractEpoch,
            categoryCountsKnown: requireCategories);
        classification = RecoveryClassification.TrustedComplete;
        return true;
    }
}
