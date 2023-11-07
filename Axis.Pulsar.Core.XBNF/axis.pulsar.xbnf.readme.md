# Axis.Pulsar.Core.XBNF

### Composite Rules


### Atomic Rules
These represent conventional terminal rules - rules that read from the token stream and match them based on some defined "rule".
Atomic rules produce instances of `ICSTNode.Terminal` - Concrete syntax tree nodes that have no children nodes.

__XBNF__ implements _Atomic Rules_ as pluggable components that implement the `Axis.Pulsar.Core.XBNF.IAtomicRuleFactory` interface.
These components are then introduced into the Grammar


