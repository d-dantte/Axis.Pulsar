# Axis.Pulsar.Core.XBNF
> Data representation specification and implementation, supporting both textual (human readable) and binary formats.


## Contents
1. [Introduction](#Introduction)
2. [Specification](#Specification)
3. [Annotation](#Annotation)
4. [Bool](#Bool)
5. [Integer](#Int)
6. [Decimal](#Decimal)
7. [Instant](#Instant)
8. [String](#String)
9. [Symbol](#Symbol)
10. [Clob](#Clob)
11. [Blob](#Blob)
12. [List](#List)
13. [Record](#Record)
14. [Appendix](#Appendix) (things like key-words, var-bytes, etc will appear here)


## <a id="Introduction"></a> Introduction
Dia is yet another data representation format, born from the need for more feature-sets in json; as such, it is a superset of json.

## <a id="Specification"></a> Specification
Dia recognizes 9 data types. The concept of types in `Dia` allow for the absence of values: in this case, a null is used. `Dia` types are:
0. Annotation
1. Bool
2. Int
3. Decimal
4. Instant
5. String
6. Symbol
7. Clob
8. Blob
9. List
10. Record

Every dia value represents data of the corresponding type, or the absence of data. All values may also have an optional annotation list attached to them.

As already stated, Dia supports Textual and Binary representation of a specific arrangement of the above types. The following sections will
discuss the details of each of the types, as well as their representation in the 2 formats. However, before proceeding, a general overview of
the binary format is necessary, as there are shared concepts among the types that need to be established first.
