# Cet.NATS.Client
![Official NATS logo](https://github.com/nats-io/nats-site/blob/master/src/img/large-logo.png)

A C# client for NATS brokers.

> The fastest yet simplest way to exchange messages (after hairdressers, of course).

## Intro
This piece of software was created as a spin-off of the official Synadia's NATS C# client: https://github.com/nats-io/csharp-nats

From the original sources, the resulting code was almost completely rewritten, having the latest .Net features in mind. Motivations were: performance, low-resources environments, and thread-safety.

Main credits go to Synadia, which is the NATS project owner.

## Features
Here are some of the features:
- blazing fast
- concurrent usage (thread-safety)
- the library targets .Net Standard 2.0
- passive and reactive consumer interface
- sync and async interface

> NOTE: this library exposes APIs which are different from the original version.

## Installation
The recommended way to install is via NuGet: https://www.nuget.org/packages/Cet.NATS.Client/

## Usage
Here are just some examples on how to use.

### NATS server setup
In order to make any client code working, you must first setup and run the NATS server. More info here: https://nats.io/download/nats-io/gnatsd/

From the clients, the outgoing connections are TCP/IP against the machine where the NATS server runs. To make tests easier, you may run the server on the local machine.


### Basic pub-sub (passive pattern)
The publisher sends a simple string message to the NATS broker without any acknowledgement (fire-and-forget). All the subscribers matching the target subject will receive the original message.

```C#
    class DemoPubSubPassive
    {
        private const string TestPayload = "Hello world!";

        public void Run()
        {
            //starts two kinds of subscribers
            Task.Factory.StartNew(this.SubscriberSync);
            Task.Factory.StartNew(this.SubscriberAsync);

            //starts the publisher
            this.Publisher();
        }

        private void Publisher()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends a single message having a string as payload
                Console.WriteLine("Publishing: " + TestPayload);
                conn.Publish(
                    new MsgIn("The.Target").SetPayload(TestPayload),
                    CancellationToken.None
                    );
            }
        }

        /// <summary>
        /// The sync-way subscriber
        /// </summary>
        private void SubscriberSync()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //subscribe to the subject
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //waits the message
                MsgOut m = sub.NextMessage(CancellationToken.None);

                //verify the expectation
                Console.WriteLine("Sync received: " + m.GetPayloadAsString());
                Debug.Assert(m.GetPayloadAsString() == TestPayload);
            }
        }

        /// <summary>
        /// The async-way subscriber
        /// </summary>
        /// <returns></returns>
        private async Task SubscriberAsync()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //subscribe to the subject
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //waits the message
                MsgOut m = await sub.NextMessageAsync(CancellationToken.None);

                //verify the expectation
                Console.WriteLine("Async received: " + m.GetPayloadAsString());
                Debug.Assert(m.GetPayloadAsString() == TestPayload);
            }
        }
    }
```


### Basic pub-sub (reactive pattern)
Same as before, but here is a different way to consume the incoming messages. Each consumer gets notified as soon a new message is ready to dispatch.

```C#
        /// <summary>
        /// The sync-handler subscriber
        /// </summary>
        private void Subscriber1()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //subscribe to the subject
                IReactiveSubscription sub = conn.SubscribeReactive("The.>", Sub1Handler);

                //waits here until the semaphore will be released
                _sem1.Wait();
            }
        }

        private SemaphoreSlim _sem1 = new SemaphoreSlim(0);

        private void Sub1Handler(MsgOut m, CancellationToken token)
        {
            Console.WriteLine("Sub1 received: " + m.GetPayloadAsString());
            Debug.Assert(m.GetPayloadAsString() == TestPayload);

            //releases the semaphore
            _sem1.Release();
        }
```
> NOTE: the code highlights only one consumer, for clarity.

Although the async-way of the reactive pattern is also available, it application is discouraged whereas the original publisher's sequence of message should be guaranteed.


### Basic request-reply
The "master" client sends a request as a simple string message to the NATS broker, and halts waiting for a response. The "slave" subscriber matching the target subject will receive the original message, then sends a response back. Once the response will be dispatched to the master client, it continues the regular program flow.

```C#
    class DemoReqRepSync
    {
        public void Run()
        {
            //starts the slave
            Task.Factory.StartNew(this.Slave);

            //starts the master
            this.Master();
        }

        private string _expectedResponsePayload;

        private void Master()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                //sends a request and waits for the response
                MsgOut m = conn.Request(
                    new MsgIn("The.Target").SetPayload("Mario"),
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None
                    );

                Console.WriteLine("Master received: " + m.GetPayloadAsString());
                Debug.Assert(m.GetPayloadAsString() == this._expectedResponsePayload);
            }
        }

        private void Slave()
        {
            ClientOptions opts = ConnectionUtils.GetDefaultOptions();
            ConnectionFactory cf = new ConnectionFactory();
            using (IConnection conn = cf.CreateConnection(opts))
            {
                IPassiveSubscription sub = conn.SubscribePassive("The.>");

                //waits for a request
                MsgOut m = sub.NextMessage(CancellationToken.None);
                Console.WriteLine("Slave received: " + m.GetPayloadAsString());

                //builds the response up, then publish it back as reply
                this._expectedResponsePayload = "Hello " + m.GetPayloadAsString() + "!";
                conn.Publish(
                    new MsgIn(m.ReplyTo).SetPayload(this._expectedResponsePayload),
                    CancellationToken.None
                    );
            }
        }
    }
```


## Benchmarks
Here are some performance results comparing this and the original NATS client.

The source code of the above tests is in the *Cet.NATS.Client.DemoPerfCompare* project.
The benchmarking environment is:
- Windows 10 Pro x64
- CPU AMD FX-6100 3.3G 3-cores
- 8G RAM
- NATS server running on the local machine
- Clients running in "Release" mode

### Test 1: pub-sub (passive-sync)
A single publisher sends N=100k different messages of about 1k bytes. There are five subscription to the same subjects, so that each consumer will receive a copy of the message, and verifies it.

| Cet.NATS.Client (this) | |
| --- | --- |
| duration | 12.1s |
| data rate | 9.9MB/s |
| RAM | 63MB |
| CPU (avg) | 50% |

| NATS.Client (original) | |
| --- | --- |
| duration | 10.1s |
| data rate | 11.8MB/s |
| RAM | 354MB |
| CPU (avg) | 60% |

### Test 2: request-reply
A single "slave" with 5 (five) subscriptions, each to a different subject. Then 50 (fifty) "masters" requesting against the same "slave". A group of 10 (ten) masters send requests to the same subject.

Each master sends N=1000 different messages of about 1k bytes. The slave makes a little change on the received request, the replies it back. Finally, the master checks for the expected response.

| Cet.NATS.Client (this) | |
| --- | --- |
| duration | 21.3s |
| data rate | 5.6MB/s |
| RAM | 56MB |
| CPU (avg) | 15% |

| NATS.Client (original) | |
| --- | --- |
The app stalls or timeouts

## License
MIT license: https://opensource.org/licenses/MIT
