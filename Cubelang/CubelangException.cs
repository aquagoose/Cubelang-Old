using System;

namespace Cubelang;

public class CubelangException : Exception
{
    public CubelangException(int line, string message) : base(message) { }
}