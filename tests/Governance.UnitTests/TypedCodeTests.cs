using System.Text.Json;
using Governance.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Xunit;

namespace Governance.UnitTests;

public sealed class TypedCodeTests
{
    [Fact]
    public void Legacy_store_result_keeps_its_wire_shape()
    {
        const string legacy = "{\"Code\":\"Ok\",\"Action\":null}";
        var result = JsonSerializer.Deserialize<StoreResult>(legacy)!;

        Assert.Equal(StoreResultCode.Ok, result.Code);
        Assert.True(result.IsSuccess);
        Assert.Equal(legacy, JsonSerializer.Serialize(result));
        Assert.False(new StoreResult(default, null).IsSuccess);
    }

    [Theory]
    [InlineData("preview", EvaluationPhase.Preview)]
    [InlineData("submission", EvaluationPhase.Submission)]
    [InlineData("pre-execution", EvaluationPhase.PreExecution)]
    public void Evaluation_phase_preserves_legacy_spelling(string text, EvaluationPhase phase)
    {
        var json = JsonSerializer.Serialize(text);
        Assert.Equal(phase, JsonSerializer.Deserialize<EvaluationPhase>(json));
        Assert.Equal(json, JsonSerializer.Serialize(phase));
    }

    [Fact]
    public void Legacy_audit_event_reads_string_type_and_reason()
    {
        const string legacy = """
            {
              "Id":"11111111-1111-1111-1111-111111111111",
              "TenantId":"tenant-a",
              "ActionId":"22222222-2222-2222-2222-222222222222",
              "Sequence":1,
              "Type":"Claim",
              "At":"2026-09-12T12:00:00+00:00",
              "ActorId":"worker",
              "CorrelationId":"legacy",
              "Code":"ExecutionStarted",
              "SafeMetadataJson":"{}"
            }
            """;
        var audit = JsonSerializer.Deserialize<AuditEvent>(legacy)!;
        Assert.Equal(AuditEventType.Claim, audit.Type);
        Assert.Equal<ReasonCode>(RuntimeReasonCode.ExecutionStarted, audit.Code);

        using var serialized = JsonDocument.Parse(JsonSerializer.Serialize(audit));
        Assert.Equal("Claim", serialized.RootElement.GetProperty("Type").GetString());
        Assert.Equal("ExecutionStarted", serialized.RootElement.GetProperty("Code").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("\"0\"")]
    [InlineData("\"FutureValue\"")]
    public void Closed_protocol_enums_reject_ordinals_and_unknown_names(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StoreResultCode>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EvaluationPhase>(json));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AuditEventType>(json));
    }

    [Fact]
    public void Application_reason_is_extensible_and_round_trips_without_core_registration()
    {
        var reason = new ReasonCode("billing.invoice-locked");
        var result = new ExecutionResult(Effect.NotApplied, reason);
        var restored = JsonSerializer.Deserialize<ExecutionResult>(JsonSerializer.Serialize(result))!;

        Assert.Equal(reason, restored.Code);
        Assert.Contains("\"Code\":\"billing.invoice-locked\"", JsonSerializer.Serialize(restored));
        Assert.NotEqual(new ReasonCode("billing.invoice-unlocked"), restored.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains whitespace")]
    [InlineData("contains\nnewline")]
    public void Invalid_reason_is_rejected_without_echoing_input(string value)
    {
        var error = Assert.Throws<ArgumentException>(() => new ReasonCode(value));
        if (value.Length > 0)
        {
            Assert.DoesNotContain(value, error.Message);
        }

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ReasonCode>(JsonSerializer.Serialize(value)));
    }

    [Fact]
    public void Reserved_enum_conversion_rejects_undefined_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (ReasonCode)(RuntimeReasonCode)999);
        Assert.Throws<ArgumentOutOfRangeException>(() => (ReasonCode)(StoreResultCode)999);
        Assert.Equal<ReasonCode>(RuntimeReasonCode.Unspecified, default(ReasonCode));
    }
}
