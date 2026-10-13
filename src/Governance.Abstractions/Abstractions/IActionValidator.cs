using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface IActionValidator<T>
{
    ValidationResult<T> Validate(T payload);
}

