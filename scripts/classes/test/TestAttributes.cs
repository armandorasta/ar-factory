using System;

namespace ArFactory.Tests;

[AttributeUsage(AttributeTargets.Method)] 
public class TestAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class TestSuiteAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RunThisOnlyAttribute : Attribute { }
