using System;

namespace ArFactory.Tests;

public class AssertFailedException(string msg) : Exception(msg)
{
}
