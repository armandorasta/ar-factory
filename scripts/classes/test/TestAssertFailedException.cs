using System;

namespace ArTest;

public class TestAssertFailedException(string msg) : Exception(msg)
{
}
