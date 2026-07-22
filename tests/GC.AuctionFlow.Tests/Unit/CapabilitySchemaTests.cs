using System.Collections.Immutable;
using System.Text.Json;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Data;

namespace GC.AuctionFlow.Tests.Unit;

public sealed class CapabilityEnumSerializationTests
{
    [Theory]
    [InlineData(typeof(DataState))]
    [InlineData(typeof(DataSourceMode))]
    [InlineData(typeof(DataSourceModeProvenance))]
    [InlineData(typeof(EvidenceProvenance))]
    [InlineData(typeof(CapabilityKind))]
    [InlineData(typeof(CapabilityAvailability))]
    [InlineData(typeof(ComputabilityState))]
    [InlineData(typeof(FidelityState))]
    [InlineData(typeof(SequenceEvidence))]
    [InlineData(typeof(MboLifecycleState))]
    [InlineData(typeof(TradeStreamKind))]
    public void Enum_values_serialize_as_camelCase_strings(Type enumType)
    {
        var options = CapabilityJson.CreateOptions();
        foreach (var value in Enum.GetValues(enumType))
        {
            var json = JsonSerializer.Serialize(value, enumType, options);
            Assert.StartsWith("\"", json);
            Assert.EndsWith("\"", json);
            Assert.False(int.TryParse(json.Trim('"'), out _), $"Expected string enum for {value}");
            var round = JsonSerializer.Deserialize(json, enumType, options);
            Assert.Equal(value, round);
        }
    }

    [Fact]
    public void DataCoverage_flags_round_trip()
    {
        var options = CapabilityJson.CreateOptions();
        const DataCoverage value = DataCoverage.Live | DataCoverage.Historical;
        var json = JsonSerializer.Serialize(value, options);
        var round = JsonSerializer.Deserialize<DataCoverage>(json, options);
        Assert.Equal(value, round);
    }
}

public sealed class CapabilitySnapshotTests
{
    [Fact]
    public void Snapshot_round_trip_preserves_cells_and_evidence()
    {
        var snap = CreateValidSnapshot();
        var dto = CapabilitySnapshotDto.FromSnapshot(snap);
        var json = CapabilityJson.SerializeSnapshot(dto);
        Assert.Contains("\"schemaVersion\":\"1.0.0\"", json, StringComparison.Ordinal);
        Assert.Contains("\"probeVersion\":\"0.0.6\"", json, StringComparison.Ordinal);
        Assert.Contains("\"noNativeSequence\"", json, StringComparison.Ordinal);

        var back = CapabilityJson.DeserializeSnapshot(json);
        Assert.NotNull(back);

        var result = back!.TryToSnapshot(out var restored);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(restored);
        Assert.Equal(snap.SchemaVersion, restored!.SchemaVersion);
        Assert.Equal(snap.ProbeVersion, restored.ProbeVersion);
        Assert.Equal(snap.SessionId, restored.SessionId);
        Assert.Equal(snap.InstrumentId, restored.InstrumentId);
        Assert.Equal(snap.DataState, restored.DataState);
        Assert.Equal(snap.DataSourceMode, restored.DataSourceMode);
        Assert.Equal(snap.Capabilities.Count, restored.Capabilities.Count);
        Assert.True(restored.TryGet(CapabilityKind.Trades, out var trades));
        Assert.Equal(SequenceEvidence.NoNativeSequence, trades.Sequence);
        Assert.Equal(FidelityState.Unknown, trades.Fidelity);
    }

    [Fact]
    public void Duplicate_capability_kind_is_rejected()
    {
        var cells = new[]
        {
            SampleCell(CapabilityKind.Trades),
            SampleCell(CapabilityKind.Trades)
        };

        var result = CapabilitySnapshot.TryCreate(
            CapabilitySchemaVersions.SchemaVersion,
            CapabilitySchemaVersions.ProbeVersionPlaceholder,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "GC",
            DataState.Degraded,
            DataSourceMode.Live,
            DataSourceModeProvenance.OperatorDeclared,
            cells,
            null,
            null,
            out var snapshot);

        Assert.False(result.IsValid);
        Assert.Null(snapshot);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public void Unsupported_schema_version_is_rejected()
    {
        var result = CapabilitySnapshot.TryCreate(
            "9.9.9",
            CapabilitySchemaVersions.ProbeVersionPlaceholder,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "GC",
            DataState.Invalid,
            DataSourceMode.Live,
            DataSourceModeProvenance.OperatorDeclared,
            Array.Empty<CapabilityCell>(),
            null,
            null,
            out var snapshot);

        Assert.False(result.IsValid);
        Assert.Null(snapshot);
        Assert.Contains(result.Errors, e => e.Contains("SchemaVersion", StringComparison.Ordinal));
    }

    [Fact]
    public void Capabilities_dictionary_is_immutable_lookup()
    {
        var snap = CreateValidSnapshot();
        Assert.True(snap.TryGet(CapabilityKind.Trades, out _));
        Assert.False(snap.TryGet(CapabilityKind.OpenInterest, out _));
        Assert.IsAssignableFrom<ImmutableDictionary<CapabilityKind, CapabilityCell>>(snap.Capabilities);
        Assert.Throws<NotSupportedException>(() =>
        {
            var mutable = (IDictionary<CapabilityKind, CapabilityCell>)snap.Capabilities;
            mutable.Add(CapabilityKind.OpenInterest, SampleCell(CapabilityKind.OpenInterest));
        });
    }

    [Fact]
    public void Instrument_identity_remains_explicit_on_snapshot()
    {
        var snap = CreateValidSnapshot(instrumentId: "ES");
        Assert.Equal("ES", snap.InstrumentId);
        Assert.NotEqual("GC", snap.InstrumentId);
    }

    [Fact]
    public void Tests_reference_production_assembly_not_mirrored_source()
    {
        var production = typeof(CapabilitySnapshot).Assembly.GetName().Name;
        var tests = typeof(CapabilitySnapshotTests).Assembly.GetName().Name;
        Assert.Equal("GC.AuctionFlow", production);
        Assert.Equal("GC.AuctionFlow.Tests", tests);
        Assert.NotEqual(production, tests);
    }

    private static CapabilitySnapshot CreateValidSnapshot(string instrumentId = "GC")
    {
        var evidence = new EvidenceArtifactIdentity(
            ArtifactNameOrPath: "docs/evidence/CapabilityMatrix_ES_2026-07-20.json",
            FileSizeBytes: 421913,
            Sha256Hex: "471BDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC1",
            SchemaVersion: "1.0.0",
            ProbeVersion: "0.1.0",
            SessionId: "7d5e58b17eb54438a45b1b8efb72a5f1",
            GeneratedUtc: DateTimeOffset.Parse("2026-07-20T15:38:50.5210229Z"),
            InstrumentId: "ES");

        var result = CapabilitySnapshot.TryCreate(
            CapabilitySchemaVersions.SchemaVersion,
            CapabilitySchemaVersions.ProbeVersionPlaceholder,
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            DateTimeOffset.Parse("2026-07-21T00:00:00Z"),
            instrumentId,
            DataState.Degraded,
            DataSourceMode.Live,
            DataSourceModeProvenance.OperatorDeclared,
            new[]
            {
                SampleCell(CapabilityKind.Trades),
                SampleCell(CapabilityKind.Mbo) with
                {
                    MboLifecycle = MboLifecycleState.EventPresenceOnly,
                    Availability = CapabilityAvailability.Partial,
                    Fidelity = FidelityState.Unknown,
                    NotesOrLimitationCodes = new[] { KnownLimitationCodes.MboEventPresenceNotLifecycle }
                }
            },
            new[] { KnownLimitationCodes.GcRuntimeCapabilityUnproven },
            new[] { evidence },
            out var snapshot);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        return snapshot!;
    }

    internal static CapabilityCell SampleCell(CapabilityKind kind) =>
        new(
            kind,
            CapabilityAvailability.Partial,
            DataCoverage.Live,
            ComputabilityState.Unknown,
            FidelityState.Unknown,
            SequenceEvidence.NoNativeSequence,
            EvidenceProvenance.ObservedApi,
            DateTimeOffset.Parse("2026-07-21T00:00:00Z"),
            new[] { KnownLimitationCodes.NoNativeSequence },
            kind == CapabilityKind.Mbo ? MboLifecycleState.EventPresenceOnly : null);
}

public sealed class EvidenceArtifactIdentityTests
{
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("471BDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC")]
    [InlineData("471BDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC1FF")]
    [InlineData("ZZZBDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC1")]
    public void Invalid_sha256_is_rejected(string sha)
    {
        var id = new EvidenceArtifactIdentity(
            "artifact.json", 1, sha, "1.0.0", "0.0.3", "s", DateTimeOffset.UtcNow, "ES");
        Assert.False(id.HasValidSha256Format);
        Assert.False(id.ValidateContract().IsValid);
    }

    [Fact]
    public void Null_sha256_is_allowed_on_contract()
    {
        var id = new EvidenceArtifactIdentity(
            "artifact.json", 1, null, "1.0.0", "0.0.3", "s", DateTimeOffset.UtcNow, "ES");
        Assert.True(id.ValidateContract().IsValid);
    }

    [Fact]
    public void Valid_sha256_is_accepted()
    {
        var id = new EvidenceArtifactIdentity(
            "docs/evidence/CapabilityMatrix_ES_2026-07-20.json",
            421913,
            "471BDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC1",
            "1.0.0",
            "0.1.0",
            "7d5e58b17eb54438a45b1b8efb72a5f1",
            DateTimeOffset.UtcNow,
            "ES");
        Assert.True(id.HasValidSha256Format);
        Assert.True(id.ValidateContract().IsValid);
    }

    [Fact]
    public void Es_evidence_cannot_be_relabeled_as_gc()
    {
        var es = new EvidenceArtifactIdentity(
            "CapabilityMatrix_ES.json", 1,
            "471BDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC1",
            "1.0.0", "0.1.0", "sid", DateTimeOffset.UtcNow, "ES");

        var result = EvidenceArtifactIdentity.RejectInstrumentRelabel(es, "GC");
        Assert.False(result.IsValid);
        Assert.Contains(KnownLimitationCodes.EsEvidenceNotGcProof, result.Errors[0]);
    }
}

public sealed class CapabilitySemanticsTests
{
    [Fact]
    public void Unknown_mode_cannot_masquerade_as_live()
    {
        Assert.False(CapabilitySemantics.IsLive(DataSourceMode.Unknown));
        Assert.True(CapabilitySemantics.IsLive(DataSourceMode.Live));
    }

    [Fact]
    public void Mode_without_provenance_is_unresolved()
    {
        Assert.False(CapabilitySemantics.IsModeResolved(DataSourceMode.Live, DataSourceModeProvenance.Unknown));
        Assert.False(CapabilitySemantics.IsModeResolved(DataSourceMode.Unknown, DataSourceModeProvenance.OperatorDeclared));
        Assert.True(CapabilitySemantics.IsModeResolved(DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared));
    }

    [Fact]
    public void Live_with_unknown_provenance_rejected_for_declaration()
    {
        var result = CapabilitySemantics.ValidateDataSourceModeDeclaration(
            DataSourceMode.Live, DataSourceModeProvenance.Unknown);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Explicit_unknown_mode_may_be_stored_with_unknown_provenance()
    {
        var result = CapabilitySnapshot.TryCreate(
            CapabilitySchemaVersions.SchemaVersion,
            CapabilitySchemaVersions.ProbeVersionPlaceholder,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "ES",
            DataState.Degraded,
            DataSourceMode.Unknown,
            DataSourceModeProvenance.Unknown,
            Array.Empty<CapabilityCell>(),
            new[] { KnownLimitationCodes.UnknownModeNotLive },
            null,
            out var snapshot);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(snapshot);
        Assert.Equal(DataSourceMode.Unknown, snapshot!.DataSourceMode);
        Assert.False(CapabilitySemantics.IsModeResolved(snapshot.DataSourceMode, snapshot.DataSourceModeProvenance));
    }

    [Fact]
    public void NoNativeSequence_does_not_force_Invalid()
    {
        Assert.False(CapabilitySemantics.DoesNoNativeSequenceForceInvalid(SequenceEvidence.NoNativeSequence));
        Assert.False(CapabilitySemantics.ImpliesExchangeFeedCompleteness(SequenceEvidence.NoNativeSequence));
        Assert.False(CapabilitySemantics.ImpliesExchangeFeedCompleteness(SequenceEvidence.LocalSequenceOnly));
        Assert.True(CapabilitySemantics.ImpliesExchangeFeedCompleteness(SequenceEvidence.NativeSequenceAvailable));
    }

    [Fact]
    public void Mbo_event_presence_is_not_lifecycle_completeness()
    {
        Assert.False(CapabilitySemantics.IsMboLifecycleComplete(MboLifecycleState.EventPresenceOnly));
        Assert.True(CapabilitySemantics.IsMboLifecycleComplete(MboLifecycleState.LifecycleValidated));

        var bad = CapabilitySnapshotTests.SampleCell(CapabilityKind.Mbo) with
        {
            MboLifecycle = MboLifecycleState.EventPresenceOnly,
            Availability = CapabilityAvailability.Available,
            Fidelity = FidelityState.Validated,
            Provenance = EvidenceProvenance.ObservedApi
        };
        var result = CapabilitySemantics.ValidateCell(bad);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Available_does_not_imply_Validated()
    {
        Assert.False(CapabilitySemantics.IsFidelityValidated(
            CapabilityAvailability.Available, FidelityState.Unknown));
        Assert.False(CapabilitySemantics.IsFidelityValidated(
            CapabilityAvailability.Available, FidelityState.Partial));
        Assert.True(CapabilitySemantics.IsFidelityValidated(
            CapabilityAvailability.Available, FidelityState.Validated));
    }

    [Fact]
    public void NewTrades_and_CumulativeTrades_remain_distinct()
    {
        Assert.True(CapabilitySemantics.AreTradeStreamsDistinct(
            TradeStreamKind.NewTrades, TradeStreamKind.CumulativeTrades));
        Assert.False(CapabilitySemantics.AreTradeStreamsDistinct(
            TradeStreamKind.NewTrades, TradeStreamKind.NewTrades));
        Assert.Equal("DISTINCT_NEW_AND_CUMULATIVE_TRADES", KnownLimitationCodes.DistinctNewAndCumulativeTrades);
    }

    [Fact]
    public void Hard_facts_cannot_use_inferred_menu_presence_provenance()
    {
        var cell = CapabilitySnapshotTests.SampleCell(CapabilityKind.Trades) with
        {
            Availability = CapabilityAvailability.Available,
            Fidelity = FidelityState.Validated,
            Provenance = EvidenceProvenance.Inferred,
            NotesOrLimitationCodes = new[] { KnownLimitationCodes.IndicatorMenuPresenceNotCapabilityEvidence }
        };
        var result = CapabilitySemantics.ValidateCell(cell);
        Assert.False(result.IsValid);
    }
}
