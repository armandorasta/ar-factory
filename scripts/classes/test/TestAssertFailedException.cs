using System;

namespace ArTest;

#if TEST

public class TestAssertFailedException(string msg) : Exception(msg)
{
}

#endif
