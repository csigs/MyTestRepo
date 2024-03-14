using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Juno.Payload.Client.Extensions
{
    internal static class LinqExtensions
    {
        /// <summary>
        /// Splits an enumerable sequence to smaller by given chunk size
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="chunkSize">
        /// The chunk size.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in the source sequence.
        /// </typeparam>
        /// <returns>
        /// The <see cref="IEnumerable{IEnumerable}"/>.
        /// </returns>
        public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than zero.", "chunkSize");
            }

            var sourceSequence = source;

            while (sourceSequence.Any())
            {
                // TODO: Optimize like ChunkAsync so that this isn't potentially O(n^2)
                yield return sourceSequence.Take(chunkSize);
                sourceSequence = sourceSequence.Skip(chunkSize);
            }
        }
    }
}
