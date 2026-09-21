using System;

namespace ArFactory.Tests;

public class TestAssertFailedException(string msg) : Exception(msg)
{
}
