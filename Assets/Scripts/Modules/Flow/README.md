# Flow

Generic flow transition module. It owns only registration, enter/exit ordering, cancellation, events, and logs.

It does not define any concrete flow names or game meaning. Projects register their own `IFlow` implementations.
