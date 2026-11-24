using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kilim.Core.Tests
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
        /// <summary>İlgili UAT ID'leri (Kilim UAT v1.1).</summary>
        public string Uat { get; }
        public TestAttribute(string uat = "") { Uat = uat; }
    }

    public sealed class AssertFailed : Exception { public AssertFailed(string m) : base(m) { } }

    public static class Assert
    {
        public static void True(bool cond, string msg = "beklenen true")
        { if (!cond) throw new AssertFailed(msg); }
        public static void False(bool cond, string msg = "beklenen false")
        { if (cond) throw new AssertFailed(msg); }
        public static void Equal<T>(T expected, T actual, string msg = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new AssertFailed($"{msg} beklenen <{expected}>, gelen <{actual}>".Trim());
        }
        public static void Throws<TEx>(Action a) where TEx : Exception
        {
            try { a(); } catch (TEx) { return; }
            throw new AssertFailed($"{typeof(TEx).Name} bekleniyordu");
        }
    }

    public static class Program
    {
        public static int Main(string[] args)
        {
            string filter = args.Length > 0 ? args[0] : null;
            var tests = typeof(Program).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Select(m => (m, attr: m.GetCustomAttribute<TestAttribute>()))
                .Where(x => x.attr != null)
                .Where(x => filter == null || x.m.DeclaringType.Name.Contains(filter) || x.m.Name.Contains(filter))
                .OrderBy(x => x.m.DeclaringType.Name).ThenBy(x => x.m.Name)
                .ToList();

            int pass = 0, fail = 0;
            foreach (var (m, attr) in tests)
            {
                string name = $"{m.DeclaringType.Name}.{m.Name}";
                string uat = string.IsNullOrEmpty(attr.Uat) ? "" : $" [{attr.Uat}]";
                try
                {
                    m.Invoke(null, null);
                    pass++;
                    Console.WriteLine($"  GEÇTİ  {name}{uat}");
                }
                catch (TargetInvocationException e)
                {
                    fail++;
                    Console.WriteLine($"  KALDI  {name}{uat}\n         {e.InnerException?.Message}");
                }
            }
            Console.WriteLine($"\n{pass} geçti, {fail} kaldı, toplam {pass + fail}");
            return fail == 0 ? 0 : 1;
        }
    }
}
