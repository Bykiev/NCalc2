using System;
using System.Collections.Generic;
using Xunit;

namespace NCalc.Tests
{
    public class Lambdas
    {
        private class Context
        {
            public int FieldA { get; set; }
            public string? FieldB { get; set; }
            public decimal FieldC { get; set; }
            public decimal? FieldD { get; set; }
            public int? FieldE { get; set; }

            public int Test(int a, int b)
            {
                return a + b;
            }

            public string Test(string a, string b)
            {
                return a + b;
            }

            public int Test(int a, int b, int c)
            {
                return a + b + c;
            }

            public double Test(double a, double b, double c)
            {
                return a + b + c;
            }

            public string Sum(string msg, params int[] numbers)
            {
                int total = 0;
                foreach (var num in numbers)
                {
                    total += num;
                }
                return msg + total;
            }

            public int Sum(params int[] numbers)
            {
                int total = 0;
                foreach (var num in numbers)
                {
                    total += num;
                }
                return total;
            }

            public int Sum(TestObject1 obj1, TestObject2 obj2)
            {
                return obj1.Count1 + obj2.Count2;
            }

            public int Sum(TestObject2 obj1, TestObject1 obj2)
            {
                return obj1.Count2 + obj2.Count1;
            }

            public int Sum(TestObject1 obj1, TestObject1 obj2)
            {
                return obj1.Count1 + obj2.Count1;
            }

            public int Sum(TestObject2 obj1, TestObject2 obj2)
            {
                return obj1.Count2 + obj2.Count2;
            }

            public double Max(TestObject1 obj1, TestObject1 obj2)
            {
                return Math.Max(obj1.Count1, obj2.Count1);
            }

            public double Min(TestObject1 obj1, TestObject1 obj2)
            {
                return Math.Min(obj1.Count1, obj2.Count1);
            }

            public class TestObject1
            {
                public int Count1 { get; set; }
            }

            public class TestObject2
            {
                public int Count2 { get; set; }
            }


            public TestObject1 CreateTestObject1(int count)
            {
                return new TestObject1() { Count1 = count };
            }

            public TestObject2 CreateTestObject2(int count)
            {
                return new TestObject2() { Count2 = count };
            }
        }

        private class SubContext : Context
        {
            public int Multiply(int a, int b)
            {
                return a * b;
            }

            public new int Test(int a, int b)
            {
                return base.Test(a, b) / 2;
            }

            public int Test(int a, int b, int c, int d)
            {
                return a + b + c + d;
            }

            public int Sum(TestObject1 obj1, TestObject2 obj2, TestObject2 obj3)
            {
                return obj1.Count1 + obj2.Count2 + obj3.Count2 + 100;
            }
        }

        [Theory]
        [InlineData("1+2", 3)]
        [InlineData("1-2", -1)]
        [InlineData("2*2", 4)]
        [InlineData("10/2", 5)]
        [InlineData("7%2", 1)]
        public void ShouldHandleIntegers(string input, int expected)
        {
            var expression = Extensions.CreateExpression(input);
            var sut = expression.ToLambda<int>();

            Assert.Equal(sut(), expected);
        }

        [Fact]
        public void ShouldHandleParameters()
        {
            var expression = Extensions.CreateExpression("[FieldA] > 5 && [FieldB] = 'test'");
            var sut = expression.ToLambda<Context, bool>();
            var context = new Context { FieldA = 7, FieldB = "test" };

            Assert.True(sut(context));
        }

        [Fact]
        public void ShouldHandleOverloadingSameParamCount()
        {
            var expression = Extensions.CreateExpression("Test('Hello', ' world!')");
            var sut = expression.ToLambda<Context, string>();
            var context = new Context();

            Assert.Equal("Hello world!", sut(context));
        }

        [Fact]
        public void ShouldHandleOverloadingDifferentParamCount()
        {
            var expression = Extensions.CreateExpression("Test(Test(1, 2), 3, 4)");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.Equal(10, sut(context));
        }

        [Fact]
        public void ShouldHandleOverloadingObjectParameters()
        {
            var expression = Extensions.CreateExpression("Sum(CreateTestObject1(2), CreateTestObject2(2)) + Sum(CreateTestObject2(1), CreateTestObject1(5))");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.Equal(10, sut(context));
        }


        [Fact]
        public void ShouldHandleParamsKeyword()
        {
            var expression = Extensions.CreateExpression("Sum(Test(1,1),2)");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.Equal(4, sut(context));
        }

        [Fact]
        public void ShouldHandleMixedParamsKeyword()
        {
            var expression = Extensions.CreateExpression("Sum('Your total is: ', Test(1,1), 2, 3)");
            var sut = expression.ToLambda<Context, string>();
            var context = new Context();

            Assert.Equal("Your total is: 7", sut(context));
        }

        [Fact]
        public void ShouldHandleCustomFunctions()
        {
            var expression = Extensions.CreateExpression("Test(Test(1, 2), 3)");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.Equal(6, sut(context));
        }

        [Fact]
        public void ShouldHandleContextInheritance()
        {
            var lambda1 = Extensions.CreateExpression("Multiply(5, 2)").ToLambda<SubContext, int>();
            var lambda2 = Extensions.CreateExpression("Test(5, 5)").ToLambda<SubContext, int>();
            var lambda3 = Extensions.CreateExpression("Test(1,2,3,4)").ToLambda<SubContext, int>();
            var lambda4 = Extensions.CreateExpression("Sum(CreateTestObject1(100), CreateTestObject2(100), CreateTestObject2(100))")
                .ToLambda<SubContext, int>();

            var context = new SubContext();
            Assert.Equal(10, lambda1(context));
            Assert.Equal(5, lambda2(context));
            Assert.Equal(10, lambda3(context));
            Assert.Equal(400, lambda4(context));
        }

        [Theory]
        [InlineData("Test(1, 1, 1)")]
        [InlineData("Test(1.0, 1.0, 1.0)")]
        [InlineData("Test(1.0, 1, 1.0)")]
        public void ShouldHandleImplicitConversion(string input)
        {
            var lambda = Extensions.CreateExpression(input).ToLambda<Context, int>();

            var context = new Context();
            Assert.Equal(3, lambda(context));
        }

        [Fact]
        public void MissingMethod()
        {
            var expression = Extensions.CreateExpression("MissingMethod(1)");
            try
            {
                var sut = expression.ToLambda<Context, int>();
            }
            catch (System.MissingMethodException ex)
            {

                System.Diagnostics.Debug.Write(ex);
                Assert.True(true);
                return;
            }
            Assert.True(false);

        }

        [Fact]
        public void ShouldHandleTernaryOperator()
        {
            var expression = Extensions.CreateExpression("Test(1, 2) = 3 ? 1 : 2");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.Equal(1, sut(context));
        }

        [Fact]
        public void Issue1()
        {
            var expr = Extensions.CreateExpression("2 + 2 - a - b - x");

            decimal x = 5m;
            decimal a = 6m;
            decimal b = 7m;

            expr.Parameters["x"] = x;
            expr.Parameters["a"] = a;
            expr.Parameters["b"] = b;

            var f = expr.ToLambda<float>(); // Here it throws System.ArgumentNullException. Parameter name: expression
            Assert.Equal(-14, f());
        }

        [Theory]
        [InlineData("if(true, true, false)")]
        [InlineData("in(3, 1, 2, 3, 4)")]
        public void ShouldHandleBuiltInFunctions(string input)
        {
            var expression = Extensions.CreateExpression(input);
            var sut = expression.ToLambda<bool>();
            Assert.True(sut());
        }

        [Theory]
        [InlineData("Min(CreateTestObject1(1), CreateTestObject1(2))", 1)]
        [InlineData("Max(CreateTestObject1(1), CreateTestObject1(2))", 2)]
        [InlineData("Min(1, 2)", 1)]
        [InlineData("Max(1, 2)", 2)]
        public void ShouldProritiseContextFunctions(string input, double expected)
        {
            var expression = Extensions.CreateExpression(input);
            var lambda = expression.ToLambda<Context, double>();
            var context = new Context();
            var actual = lambda(context);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("[FieldA] > [FieldC]", true)]
        [InlineData("[FieldC] > 1.34", true)]
        [InlineData("[FieldC] > (1.34 * 2) % 3", false)]
        [InlineData("[FieldE] = 2", true)]
        [InlineData("[FieldD] > 0", false)]
        public void ShouldHandleDataConversions(string input, bool expected)
        {
            var expression = Extensions.CreateExpression(input);
            var sut = expression.ToLambda<Context, bool>();
            var context = new Context { FieldA = 7, FieldB = "test", FieldC = 2.4m, FieldE = 2 };

            Assert.Equal(expected, sut(context));
        }

        [Theory]
        [InlineData("Min(3,2)", 2)]
        [InlineData("Min(3.2,6.3)", 3.2)]
        [InlineData("Max(2.6,9.6)", 9.6)]
        [InlineData("Max(9,6)", 9.0)]
        [InlineData("Pow(5,2)", 25)]
        public void ShouldHandleNumericBuiltInFunctions(string input, double expected)
        {
            var expression = Extensions.CreateExpression(input);
            var sut = expression.ToLambda<object>();
            Assert.Equal(expected, sut());
        }

        [Theory]
        [InlineData("if(true, 1, 0.0)", 1.0)]
        [InlineData("if(true, 1.0, 0)", 1.0)]
        [InlineData("if(true, 1.0, 0.0)", 1.0)]
        public void ShouldHandleFloatIfFunction(string input, double expected)
        {
            var expression = Extensions.CreateExpression(input);
            var sut = expression.ToLambda<object>();
            Assert.Equal(expected, sut());
        }

        [Theory]
        [InlineData("if(true, 1, 0)", 1)]
        public void ShouldHandleIntIfFunction(string input, int expected)
        {
            var expression = Extensions.CreateExpression(input);
            var sut = expression.ToLambda<object>();
            Assert.Equal(expected, sut());
        }

        [Theory]
        [InlineData("if(true, 'a', 'b')", "a")]
        public void ShouldHandleStringIfFunction(string input, string expected)
        {
            var expression = Extensions.CreateExpression(input);
            var sut = expression.ToLambda<object>();
            Assert.Equal(expected, sut());
        }

        [Fact]
        public void ShouldAllowValueTypeContexts()
        {
            // Arrange
            const decimal expected = 6.908m;
            var expression = Extensions.CreateExpression("Foo * 3.14");
            var sut = expression.ToLambda<FooStruct, decimal>();
            var context = new FooStruct();

            // Act
            var actual = sut(context);

            // Assert
            Assert.Equal(expected, actual);
        }

        // https://github.com/sklose/NCalc2/issues/54
        [Fact]
        public void Issue54()
        {
            // Arrange
            const long expected = 9999999999L;
            var expression = $"if(true, {expected}, 0)";
            var e = Extensions.CreateExpression(expression);
            var context = new object();

            var lambda = e.ToLambda<object, long>();

            // Act
            var actual = lambda(context);

            // Assert
            Assert.Equal(expected, actual);
        }

        internal struct FooStruct
        {
            public double Foo => 2.2;
        }

        struct ContextAndResult
        {
            public double x;
            public double y;
            public string Func;
            public double ExpressionResult;
            public double LambdaResult;
        }

        [Fact]
        public void ExpressionAndLambdaFuncBehaviorMatch()
        {
            // Arrange
            double[] testValues = { double.MinValue, -Math.PI, -1, Math.BitDecrement(0), 0, Math.BitIncrement(0), 0.001, 1, 2, 3.14, Math.PI, 10, 100, double.MaxValue };

            string[] functionsImplemented =
            {
                "Abs", "Acos", "Asin", "Atan", "Ceiling", "Cos", "Exp", "Floor", "IEEERemainder",
                "Log", "Log10", "Max", "Min", "Pow", "Round", "Sin", "Sqrt", "Tan", "Truncate"
            };
            HashSet<string> functionsWithTwoArguments = new HashSet<string>()
            {
                "Round", "IEEERemainder", "Log", "Max", "Min", "Pow"
            };

            string[] doubleToStringFormats = new [] { "F17", "0" };

            List<ContextAndResult> testResults = new();
            ContextAndResult currentContext = new();

            // Act
            try
            {
                // Iterate each function name with context
                foreach (string funcName in functionsImplemented)
                {
                    bool isTwoArgumentFunc = functionsWithTwoArguments.Contains(funcName);
                    string expressionString = isTwoArgumentFunc ? funcName + "(x, y)" : funcName + "(x)";

                    currentContext.Func = expressionString;

                    var expression = Extensions.CreateExpression(expressionString, EvaluateOptions.UseDoubleForAbsFunction);
                    var lambda = expression.ToLambda<ContextAndResult, double>();

                    for (int i = 0; i < testValues.Length; ++i)
                    {
                        currentContext.x = testValues[i];
                        expression.Parameters["x"] = currentContext.x;

                        if (isTwoArgumentFunc)
                        {
                            currentContext.y = testValues[(i + 1) % testValues.Length];
                            // Edge case (Round second argument is int decimal places to round from 0 to 15)
                            if (funcName == "Round")
                                currentContext.y = Math.Clamp(currentContext.y, 0, 15);
                            expression.Parameters["y"] = currentContext.y;
                        }

                        currentContext.ExpressionResult = (double)expression.Evaluate();
                        currentContext.LambdaResult = lambda(currentContext);

                        testResults.Add(currentContext);
                    }
                }

                // Iterate each function name without context with numbers of format 123.456 (doubles) or 123 (integers)
                foreach (string doubleToStringFormat in doubleToStringFormats)
                {
                    for (int i = 0; i < testValues.Length; ++i)
                    {
                        currentContext.x = testValues[i];
                        // Edge case (Exception when too big doubles not fit into Int64)
                        // We are multiplying by 0.99 because after clamping exception is still thrown
                        // Int64.MinValue = -9223372036854775808, (double)Int64.MinValue = -9223372036854780000 which is lesser)
                        if (doubleToStringFormat == "0" && Math.Abs(currentContext.x) > Int64.MaxValue)
                        {
                            currentContext.x = Math.Clamp(currentContext.x, Int64.MinValue, Int64.MaxValue) * 0.99;
                        }

                        string doubleParam1 = currentContext.x.ToString(doubleToStringFormat);

                        foreach (string funcName in functionsImplemented)
                        {
                            bool isTwoArgumentFunc = functionsWithTwoArguments.Contains(funcName);

                            string expressionString;

                            if (isTwoArgumentFunc)
                            {
                                currentContext.y = testValues[(i + 1) % testValues.Length];
                                // Edge case (see previous)
                                if (doubleToStringFormat == "0" && Math.Abs(currentContext.y) > Int64.MaxValue)
                                {
                                    currentContext.y = Math.Clamp(currentContext.y, Int64.MinValue, Int64.MaxValue) * 0.99;
                                }
                                // Edge case (Round second argument is int decimal places to round from 0 to 15)
                                if (funcName == "Round")
                                    currentContext.y = Math.Clamp(currentContext.y, 0, 15);

                                string doubleParam2 = currentContext.y.ToString(doubleToStringFormat);
                                expressionString = funcName + $"({doubleParam1}, {doubleParam2})";
                            }
                            else
                            {
                                expressionString = funcName + $"({doubleParam1})";
                            }
                            currentContext.Func = expressionString;

                            var expression = Extensions.CreateExpression(expressionString, EvaluateOptions.UseDoubleForAbsFunction);
                            var lambda = expression.ToLambda<double>();

                            currentContext.ExpressionResult = Convert.ToDouble(expression.Evaluate());
                            currentContext.LambdaResult = lambda();
                            testResults.Add(currentContext);
                        }

                    }
                }
            }
            // Serves to find an exact spot of error. Change Exception to non-related (for ex. OutOfMemoryException) to navigate via links in Test Explorer
            catch (Exception ex)
            {
                Assert.Fail($@"{ex.GetType()}, context x: {currentContext.x},
                    func: {currentContext.Func},
                    Expression result: {currentContext.ExpressionResult},
                    Lambda result: {currentContext.LambdaResult}
                    exception message: {ex.Message}
                    exception stack: {ex.StackTrace}");
            }

            static bool IsEqual(double a, double b)
            {
                if (double.IsNaN(a) && double.IsNaN(b))
                {
                    return true;
                }

                return a == b;
                //double tol = Math.Clamp(Math.Max(a, b) * 1e-12, 1e-12, 1);
                //return Math.Abs(a - b) < tol;
            }

            // Assert
            foreach (ContextAndResult testContext in testResults)
            {
                Assert.True(IsEqual(testContext.ExpressionResult, testContext.LambdaResult),
                    $@"context x: {testContext.x},
                    func: {testContext.Func},
                    Expression result: {testContext.ExpressionResult},
                    Lambda result: {testContext.LambdaResult}");
            }
        }

        struct ContextWithOverridenMethods
        {
            public double x;
            public double y;

            public double Cos(double val) => Math.Sin(val) + 1;

            public double Log(double val) => Math.Sin(val) + 2;

            public double Log(double val1, double val2) => Math.Sin(val1) + 3;
        }

        [Fact]
        public void LambdaOverrideMathFunction()
        {
            // Arrange
            ContextWithOverridenMethods context = new() { x = 3.5, y = 2.5 };

            // Not overriden function
            var expressionAbs = Extensions.CreateExpression("Abs(x)");
            var lambdaAbs = expressionAbs.ToLambda<ContextWithOverridenMethods, double>();

            // Overriden functions
            var expressionCos = Extensions.CreateExpression("Cos(x)");
            var lambdaCos = expressionCos.ToLambda<ContextWithOverridenMethods, double>();

            var expressionLog1 = Extensions.CreateExpression("Log(x)");
            var lambdaLog1 = expressionLog1.ToLambda<ContextWithOverridenMethods, double>();

            var expressionLog2 = Extensions.CreateExpression("Log(x, y)");
            var lambdaLog2 = expressionLog2.ToLambda<ContextWithOverridenMethods, double>();

            // Act
            double actualAbs = lambdaAbs(context);
            double expectedAbs = Math.Abs(context.x);

            double actualCos = lambdaCos(context);
            double expectedCos = context.Cos(context.x);

            double actualLog1 = lambdaLog1(context);
            double expectedLog1 = context.Log(context.x);

            double actualLog2 = lambdaLog2(context);
            double expectedLog2 = context.Log(context.x, context.y);

            // Assert
            Assert.Equal(expectedAbs, actualAbs);
            Assert.Equal(expectedCos, actualCos);
            Assert.Equal(expectedLog1, actualLog1);
            Assert.Equal(expectedLog2, actualLog2);
        }

    }
}
