namespace GC.AuctionFlow.Foundation;

/// <summary>
/// TTS §9.1 + audit correction 3.1: a versioned session-template *contract*. M1 does NOT pick venue
/// hours, a primary-session window, holiday rules, or a timezone representation and call them approved
/// — that is CONF-001, an owner/reviewer decision. This type only carries the identity/version/status
/// and the ETH vs primary-session *identities* separately, and fails closed when not approved for
/// production. The historical <c>PRIMARY_ANCHORED_24H_PROFILE</c> is preserved only with explicit
/// legacy/review-required provenance and is never relabeled as the Globex/primary session.
/// </summary>
public sealed record SessionTemplate(
    string TemplateId,
    string TemplateVersion,
    string TimeZoneId,
    string EthSessionIdentity,
    string PrimarySessionIdentity,
    string CalendarVersion,
    SessionTemplateStatus Status,
    string Provenance)
{
    /// <summary>
    /// The legacy anchor the current profile engine uses. Explicitly ReviewRequired — it is NOT the
    /// venue Globex session and NOT an approved primary session. Present only for compatibility.
    /// </summary>
    public static SessionTemplate LegacyPrimaryAnchored24h { get; } = new(
        TemplateId: "PRIMARY_ANCHORED_24H_PROFILE",
        TemplateVersion: "legacy",
        TimeZoneId: PrimaryAuctionClockTimeZone.IanaAmericaNewYork,
        EthSessionIdentity: "UNSPECIFIED_ETH",
        PrimarySessionIdentity: "UNSPECIFIED_PRIMARY",
        CalendarVersion: "none",
        Status: SessionTemplateStatus.ReviewRequired,
        Provenance: "legacy 24h anchor; CONF-001 unresolved; not the venue session; review required");

    /// <summary>An explicit unknown template. Session-dependent outputs are blocked.</summary>
    public static SessionTemplate Unknown { get; } = new(
        TemplateId: "UNKNOWN",
        TemplateVersion: "none",
        TimeZoneId: "UNKNOWN",
        EthSessionIdentity: "UNKNOWN",
        PrimarySessionIdentity: "UNKNOWN",
        CalendarVersion: "none",
        Status: SessionTemplateStatus.UnknownTemplate,
        Provenance: "no session template supplied");

    /// <summary>Only an ApprovedForProduction template may back session-dependent production output.</summary>
    public bool IsApprovedForProduction => Status == SessionTemplateStatus.ApprovedForProduction;

    /// <summary>Reason code when this template cannot back session-dependent output; null when it can.</summary>
    public string? BlockingReasonCode() => Status switch
    {
        SessionTemplateStatus.ApprovedForProduction => null,
        SessionTemplateStatus.UnknownTemplate => FoundationReasonCodes.SessionTemplateUnknown,
        SessionTemplateStatus.ReviewRequired => FoundationReasonCodes.SessionTemplateReviewRequired,
        _ => FoundationReasonCodes.SessionTemplateNotApprovedForProduction,
    };

    /// <summary>Stable, order-fixed serialization contributed to the deterministic foundation hash.</summary>
    public string Canonical() =>
        $"tpl={TemplateId}@{TemplateVersion};tz={TimeZoneId};eth={EthSessionIdentity};prim={PrimarySessionIdentity};cal={CalendarVersion};status={(int)Status}";
}

/// <summary>
/// Local mirror of the session timezone id so the Foundation layer carries no dependency on the
/// higher Profile module. The engine's <c>AuctionTimeZone</c> resolves the actual TimeZoneInfo;
/// M1 does not resolve venue hours (CONF-001).
/// </summary>
public static class PrimaryAuctionClockTimeZone
{
    public const string IanaAmericaNewYork = "America/New_York";
}
