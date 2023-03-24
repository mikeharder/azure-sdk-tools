using System.Collections.Generic;

namespace Azure.Sdk.Tools.TypeSpecValidator.Rules.TypeSpec
{
    internal interface TypeSpecRule
    {
        RuleResult Evaluate(TypeSpecProject typeSpecProject, IEnumerable<SwaggerFile> swaggerFiles);
    }
}
