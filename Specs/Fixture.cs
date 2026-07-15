// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Prompter.Specs;

public static class Fixture
{
    public static string Load(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Fixture '{name}' not found as an embedded resource");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
