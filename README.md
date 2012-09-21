seda-homework
=============

> Rinat Abdullin 2012-09-22

This is my attempt in learning core principles behind [EventStore](https://github.com/EventStore/EventStore), starting from [Staged Event-Driven Architecture](http://en.wikipedia.org/wiki/Staged_event-driven_architecture). 

I'm taking core bits from ES and rewrite them from scratch, while focusing on simplicity.

So far we've got:

* In-Memory Message Bus (pipes)
* Generic Finite State Machine (with builder)
* Node controller with wired custom state machine and a few states.
* In-memory timer service (for callbacks, async IO and timeouts)
* Hello World service, that properly initializes, shuts down and sends Hello to itself once every few seconds