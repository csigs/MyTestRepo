using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using Juno.Payload.Client.Abstraction;

namespace Juno.Payload.Client.Extensions
{
    public static class PayloadClientExtensions
    {
        /// <summary>
        /// Waits until logical payload is available and visible for all clients (note that create has to be called before).
        /// <param name="client">payload client</param>
        /// <param name="timeout">max waiting time</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Task</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task WaitUntilPayloadAvailableAsync(this IPayloadClientV2 client, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if(timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Value has to be greater than 0.", nameof(timeout));
            }

            bool waiting = true;
            DateTimeOffset start = DateTimeOffset.UtcNow;
            while (waiting)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await client.ExistsAsync(cancellationToken).ConfigureAwait(false))
                {
                    waiting = false;
                }
                else
                {
                    if (DateTimeOffset.UtcNow - start >= timeout)
                    {
                        waiting = false;
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);                        
                    }
                }
            }
        }

        /// <summary>
        /// Creates new logical payload and waits until it's available and visible for all clients.
        /// </summary>
        /// <param name="client">payload client</param>
        /// <param name="timeout">max waiting time</param>
        /// <param name="category">category of new payload</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Task</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task CreateNewAndWaitUntilAvailableAsync(this IPayloadClientV2 client, TimeSpan timeout, string category = "Default", CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            await client.CreateNewAsync(category, cancellationToken).ConfigureAwait(false);
            await client.WaitUntilPayloadAvailableAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
    }
}
