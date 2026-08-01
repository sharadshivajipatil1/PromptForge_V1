# ConciergeAgent design

The concierge agent is now implemented as a domain-oriented orchestrator that owns:

- language detection and greeting/FAQ resolution
- chat persistence into the data store
- front-desk escalation via concierge tickets and guest-request tasks
- personalization generation based on stay history and reference data
- check-in and checkout workflows
- recommendation booking flow

It depends on the domain abstractions only:
- IGuestService for guest lookup/update
- IDataStore for persistence
- ILlmClient for narrative generation
- IReferenceDataLoader for FAQ and activity content
