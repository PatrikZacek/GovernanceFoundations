using System.Text.Json.Serialization;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Convertors;

namespace Governance.Abstractions.Enums;

[JsonConverter(typeof(StrictStringEnumConverter<EvaluationPhase>))]
public enum EvaluationPhase
{
    [JsonStringEnumMemberName("preview")]
    Preview,
    [JsonStringEnumMemberName("submission")]
    Submission,
    [JsonStringEnumMemberName("pre-execution")]
    PreExecution
}

