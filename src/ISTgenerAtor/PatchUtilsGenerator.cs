// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony


namespace ISTgenerAtor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class PatchUtilsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext incrementalContext)
    {
        var classExistsProvider = incrementalContext.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { Identifier.Text: "PatchUtils" },
                transform: static (context, t) =>
                    context.SemanticModel.GetDeclaredSymbol(context.Node, t) as INamedTypeSymbol)
            .Where(static symbol => symbol is not null)
            .Collect();

        incrementalContext.RegisterSourceOutput(classExistsProvider, (context, classSymbols) =>
        {
            if (!classSymbols.Any())
            {
                return;
            }

            string[] codes = [
                GenerateBasicMethod(),
                GenerateCoefficientsMethod("GetCoefficients", [
                    0xff, 0x49, 0x53, 0x54, 0x41, 0x2d, 0x50, 0x61, 0x74, 0x63, 0x68, 0x65, 0x72,
                ]), GenerateCoefficientsMethod("GetConfigCoefficients", [
                    0xff, 0x50, 0x6f, 0x77, 0x65, 0x72, 0x65, 0x64, 0x20, 0x62, 0x79, 0x20, 0x7b, 0x30, 0x7d, 0x20,
                    0x7b, 0x31, 0x7d,
                ]), GenerateCoefficientsMethod("GetSourceCoefficients", [
                    0xff, 0x68, 0x74, 0x74, 0x70, 0x73, 0x3a, 0x2f, 0x2f, 0x67, 0x69, 0x74, 0x68, 0x75, 0x62, 0x2e,
                    0x63, 0x6f, 0x6d, 0x2f, 0x74, 0x61, 0x75, 0x74, 0x63, 0x6f, 0x6e, 0x79, 0x2f, 0x49, 0x53, 0x54,
                    0x41, 0x2d, 0x50, 0x61, 0x74, 0x63, 0x68, 0x65, 0x72,
                ]),
            ];

            context.AddSource(
                "ISTAlter.Core.PatchUtils.g.cs",
                SourceText.From(string.Join("\n", codes), Encoding.UTF8));
        });
    }

    private static string GenerateBasicMethod()
    {
        return $@"// <auto-generated/>
namespace ISTAlter.Core;

using System.Text;
using ISTAlter.Utils;

public static partial class PatchUtils
{{
    public static byte[] Config => string.Format(GetConfigCoefficients().GetString(18), GetCoefficients().GetString(12), Encoding.UTF8.GetString(Version)).GetBytes();

    public static byte[] Source => GetSourceCoefficients().GetString(40).GetBytes();

    public static string GetString(this int[][] coeffs, int length)
    {{
        return Encoding.UTF8.GetString(new StringGenerator(coeffs).Generate().Take(length).Where(b => b is >= 0 and <= 0xff).Select(b => (byte)b).ToArray());
    }}

    public static byte[] GetBytes(this string s)
    {{
        return Encoding.UTF8.GetBytes(s);
    }}
}}
";
    }

    private static string GenerateCoefficientsMethod(string methodName, byte[] data)
    {
        var coefficients = CalculateCoefficients(data.Skip(1).ToArray());
        var coefficientsString = string.Join(",\n        ", coefficients.Select(c => $"[{string.Join(", ", c)}]"));

        return $@"
public static partial class PatchUtils
{{
    public static int[][] {methodName}() => new int[][]
    {{
        {coefficientsString}
    }};
}}
";
    }

    private static double[] Solve(double[,] a, double[] b)
    {
        var n = b.Length;
        var x = new double[n];

        for (var i = 1; i < n; i++)
        {
            var m = a[i, i - 1] / a[i - 1, i - 1];
            a[i, i] -= m * a[i - 1, i];
            b[i] -= m * b[i - 1];
        }

        x[n - 1] = b[n - 1] / a[n - 1, n - 1];
        for (var i = n - 2; i >= 0; i--)
        {
            x[i] = (b[i] - (a[i, i + 1] * x[i + 1])) / a[i, i];
        }

        return x;
    }

    public static List<int[]> CalculateCoefficients(byte[] data)
    {
        var n = data.Length;
        var x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        var y = data.Select(b => (double)b).ToArray();
        var a = new double[n, n];
        var b = new double[n];

        a[0, 0] = 1;
        a[n - 1, n - 1] = 1;

        var h = new double[x.Length - 1];
        for (var i = 0; i < x.Length - 1; i++)
        {
            h[i] = x[i + 1] - x[i];
        }

        for (var i = 1; i < n - 1; i++)
        {
            a[i, i - 1] = h[i - 1];
            a[i, i] = 2 * (h[i - 1] + h[i]);
            a[i, i + 1] = h[i];
            b[i] = 3 * (((y[i + 1] - y[i]) / h[i]) - ((y[i] - y[i - 1]) / h[i - 1]));
        }

        var c = Solve(a, b);

        var coefficients = new List<int[]>();
        for (var i = 0; i < n - 1; i++)
        {
            var ai = Convert.ToInt32(y[i]);
            var bi = Convert.ToInt32(((y[i + 1] - y[i]) / h[i]) - (h[i] * ((2 * c[i]) + c[i + 1]) / 3));
            var ci = Convert.ToInt32(c[i]);
            var di = Convert.ToInt32((c[i + 1] - c[i]) / (3 * h[i]));
            coefficients.Add([ai, bi, ci, di]);
        }

        return coefficients;
    }
}
