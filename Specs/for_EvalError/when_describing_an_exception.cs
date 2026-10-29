// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Specs.for_EvalError;

public class when_describing_an_exception : Specification
{
    string _result = string.Empty;

    void Because() => _result = EvalError.Describe(new HttpRequestException("401 Unauthorized for key sk-ant-not-a-real-key"));

    [Fact] void should_name_the_exception_type() => _result.ShouldEqual(nameof(HttpRequestException));
    [Fact] void should_not_contain_the_message() => _result.Contains("sk-ant", StringComparison.Ordinal).ShouldBeFalse();
}
