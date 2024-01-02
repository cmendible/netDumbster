// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Xunit.Sdk;

    public class RepeatAttribute : DataAttribute
    {
        private readonly int times;

        public RepeatAttribute(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                      "Repeat count must be greater than 0.");
            }
            times = count;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return Enumerable.Repeat(Array.Empty<object>(), times);
        }
    }

}