// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints.Comparers;

namespace NUnit.Framework.Constraints
{
    /// <summary>
    /// NUnitEqualityComparer encapsulates NUnit's handling of
    /// equality tests between objects.
    /// </summary>
    public sealed class NUnitEqualityComparer
    {
        #region Static and Instance Fields
        /// <summary>
        /// If true, all string comparisons will ignore case
        /// </summary>
        private bool caseInsensitive;

        /// <summary>
        /// If true, arrays will be treated as collections, allowing
        /// those of different dimensions to be compared
        /// </summary>
        private bool compareAsCollection;

        /// <summary>
        /// Comparison objects used in comparisons for some constraints.
        /// </summary>
        private readonly List<EqualityAdapter> externalComparers = new List<EqualityAdapter>();

        /// <summary>
        /// List of points at which a failure occurred.
        /// </summary>
        private List<FailurePoint> failurePoints;

        /// <summary>
        /// List of comparers used to compare pairs of objects.
        /// </summary>
        private readonly List<IChainComparer> _comparers;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitEqualityComparer"/> class.
        /// </summary>
        public NUnitEqualityComparer()
        {
            var enumerablesComparer = new EnumerablesComparer(this);
            _comparers = new List<IChainComparer>
            {
                new ArraysComparer(this, enumerablesComparer),
                new DictionariesComparer(this),
                new DictionaryEntriesComparer(this),
                new KeyValuePairsComparer(this),
                new StringsComparer(this ),
                new StreamsComparer(this),
                new CharsComparer(this),
                new DirectoriesComparer(),
                new NumericsComparer(),
                new DateTimeOffsetsComparer(this),
                new TimeSpanToleranceComparer(),
                new TupleComparer(this),
                new ValueTupleComparer(this),
                new StructuralComparer(this),
                new EquatablesComparer(this),
                enumerablesComparer
            };
        }

        #region Properties

        /// <summary>
        /// Gets and sets a flag indicating whether case should
        /// be ignored in determining equality.
        /// </summary>
        public bool IgnoreCase
        {
            get { return caseInsensitive; }
            set { caseInsensitive = value; }
        }

        /// <summary>
        /// Gets and sets a flag indicating that arrays should be
        /// compared as collections, without regard to their shape.
        /// </summary>
        public bool CompareAsCollection
        {
            get { return compareAsCollection; }
            set { compareAsCollection = value; }
        }

        /// <summary>
        /// Gets the list of external comparers to be used to
        /// test for equality. They are applied to members of
        /// collections, in place of NUnit's own logic.
        /// </summary>
        public IList<EqualityAdapter> ExternalComparers
        {
            get { return externalComparers; }
        }

        /// <summary>
        /// Gets the list of failure points for the last Match performed.
        /// The list consists of objects to be interpreted by the caller.
        /// This generally means that the caller may only make use of
        /// objects it has placed on the list at a particular depth.
        /// </summary>
        public IList<FailurePoint> FailurePoints
        {
            get { return failurePoints; }
        }

        /// <summary>
        /// Flags the comparer to include <see cref="DateTimeOffset.Offset"/>
        /// property in comparison of two <see cref="DateTimeOffset"/> values.
        /// </summary>
        /// <remarks>
        /// Using this modifier does not allow to use the <see cref="Tolerance"/>
        /// modifier.
        /// </remarks>
        public bool WithSameOffset { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Compares two objects for equality within a tolerance.
        /// </summary>
        public bool AreEqual(object x, object y, ref Tolerance tolerance)
        {
            return AreEqual(x, y, ref tolerance, new ComparisonState(true));
        }

        internal bool AreEqual(object x, object y, ref Tolerance tolerance, ComparisonState state)
        {
            this.failurePoints = new List<FailurePoint>();

            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            if (object.ReferenceEquals(x, y))
                return true;

            if (state.DidCompare(x, y))
                return false;

            EqualityAdapter externalComparer = GetExternalComparer(x, y);
            if (externalComparer != null)
                return externalComparer.AreEqual(x, y);

            foreach (IChainComparer comparer in _comparers)
            {
                bool? result = comparer.Equal(x, y, ref tolerance, state);
                if (result.HasValue)
                    return result.Value;
            }

            return x.Equals(y);
        }

        #endregion

        #region Helper Methods

        private EqualityAdapter GetExternalComparer(object x, object y)
        {
            foreach (EqualityAdapter adapter in externalComparers)
                if (adapter.CanCompare(x, y))
                    return adapter;

            return null;
        }

        #endregion

        #region Nested FailurePoint Class

        /// <summary>
        /// FailurePoint class represents one point of failure
        /// in an equality test.
        /// </summary>
        public sealed class FailurePoint
        {
            /// <summary>
            /// The location of the failure
            /// </summary>
            public long Position;

            /// <summary>
            /// The expected value
            /// </summary>
            public object ExpectedValue;

            /// <summary>
            /// The actual value
            /// </summary>
            public object ActualValue;

            /// <summary>
            /// Indicates whether the expected value is valid
            /// </summary>
            public bool ExpectedHasData;

            /// <summary>
            /// Indicates whether the actual value is valid
            /// </summary>
            public bool ActualHasData;
        }

        #endregion
    }
}
