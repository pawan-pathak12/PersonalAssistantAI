using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace PersonalAssistantAI.Plugin;

public class CalculatorPlugin
{
      [KernelFunction("CalculatorPlugin-Add")]
    [Description("Add two numbers; parameters: a, b")]
    public double Add(double a, double b)
    {

        // Validation: Check if numbers are within reasonable range
        if (Math.Abs(a) > 1e100 || Math.Abs(b) > 1e100)
            throw new ArgumentException("Numbers too large for calculation");
        Console.WriteLine($"Calling Function : CalculatorPlugin-Add.......done");
        return a + b;
    }

    [KernelFunction("CalculatorPlugin-Subtract")]
    [Description("Subtract b from a; parameters: a, b")]
    public double Subtract(double a, double b)
    {
        Console.WriteLine($"Calling Function : CalculatorPlugin-Subtract.......done");
        var r = a - b;
        return r;
    }

    [KernelFunction("CalculatorPlugin-Multiply")]
    [Description("Multiply two numbers; parameters: a, b")]
    public double Multiply(double a, double b)
    {
        Console.WriteLine($"Calling Function : CalculatorPlugin-Multiply.......done");
        // Validation: Check for overflow
        if (a * b > double.MaxValue)
            throw new OverflowException("Multiplication result too large");

        return a * b;
    }

    [KernelFunction("CalculatorPlugin-Divide")]
    [Description("Divide a by b; parameters: a, b")]
    public double Divide(double a, double b)
    {
        if (b == 0)
        {
            Console.WriteLine("[Plugin] CalculatorPlugin-Divide received b=0, returning NaN");
            return double.NaN;
        }
        var r = a / b;
        Console.WriteLine($"Calling Function : CalculatorPlugin-Divide.......done");
        return r;
    }

    [KernelFunction("CalculatorPlugin-Square")]
    [Description("Square a number; parameter: x")]
    public double Square(double x)
    {
        Console.WriteLine($"Calling Function : CalculatorPlugin-Square.......done");
        var r = x * x;
        Console.WriteLine($"[Plugin] CalculatorPlugin-Square returning {r}");
        return r;
    }

    [KernelFunction, Description("Calculate percentage of a number")]
    public double Percentage(double number, double percentage)
    {
        Console.WriteLine("Calling plugin CalculatePlugin-Percentage....");
        return (number * percentage) / 100;
    }

    [KernelFunction, Description("Calculate power of a number")]
    public double Power(double baseNumber, double exponent)
    {
        Console.WriteLine("Calling plugin CalaculatePlugin-Power...");
        return Math.Pow(baseNumber, exponent);
    }
}
    
