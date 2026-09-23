using System;

namespace ArTest;

[AttributeUsage(AttributeTargets.Method)] 
public class TestAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RunThisOnlyAttribute : Attribute { }
