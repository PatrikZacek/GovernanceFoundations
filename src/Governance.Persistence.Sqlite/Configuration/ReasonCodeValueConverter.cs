using Governance.Abstractions.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class ReasonCodeValueConverter() : ValueConverter<ReasonCode, string>(
    code => code.Value, value => new ReasonCode(value));
