## Module Table for src/Common Utilities

| Name           | Description                                            | Dependencies                                                                                  |
| -------------- | ------------------------------------------------------ | --------------------------------------------------------------------------------------------- |
| mStd           | Core standard library utilities and foundational types | –                                                                                             |
| mSpan          | Memory-safe buffer operations with tSpan<t>            | –                                                                                             |
| mMath          | Mathematical functions and numerical operations        | –                                                                                             |
| mPerf          | Performance measurement and profiling utilities        | –                                                                                             |
| mConsole       | Extended console input/output utilities                | mStd                                                                                          |
| mError         | Centralized error handling and exception management    | mStd                                                                                          |
| mLazy          | Lazy initialization and deferred computation           | mStd                                                                                          |
| mArena         | Memory management utilities for object pooling         | mStd                                                                                          |
| mMaybe         | Optional value handling and monadic operations         | mStd, mError                                                                                  |
| mRef           | Reference wrapper and equality utilities               | mStd, mMaybe                                                                                  |
| mArenaRef      | Reference tracking and memory management               | mStd, mArena                                                                                  |
| mArenaArray    | Array-based memory allocation and reuse                | mStd, mArena, mArenaRef                                                                       |
| mArenaMaybeRef | Safe reference handling with arena-based memory        | mStd, mMaybe, mArena, mArenaRef                                                               |
| mArenaStack    | Stack-based memory allocation for performance          | mStd, mMaybe, mArena, mArenaRef, mArenaMaybeRef                                               |
| mStream        | Stream processing and transformation utilities         | mStd, mMaybe, mLazy, mRef                                                                     |
| mAssert        | Assertion utilities for debugging and testing          | mStd, mStream, mError, mConsole, mMaybe                                                       |
| mTextStream    | Text processing and streaming utilities                | mStd, mStream                                                                                 |
| mArrayList     | High-performance generic list implementation           | mStd, mStream, mMath, mAssert                                                                 |
| mAny           | Type-agnostic utilities and generic operations         | mStd, mError, mAssert                                                                         |
| mFS            | File system operations and path manipulation           | mStd, mRef, mStream, mMaybe, mError                                                           |
| mMap           | Map/dictionary utilities with advanced operations      | mStd, mStream, mMaybe                                                                         |
| mResult        | Result type for functional error handling              | mStd, mStream, mMaybe, mError                                                                 |
| mTest          | Test framework registration and utilities              | mStd, mStream, mConsole, mPerf, mError                                                        |
| mParserGen     | Parser generation and pattern-matching utilities       | mStd, mStream, mSpan, mResult, mAssert, mPerf, mMath, mMaybe                                  |
| mTextParser    | Text parsing and lexical analysis tools                | mStd, mTextStream, mMath, mPerf, mStream, mParserGen, mSpan, mError, mAssert, mResult, mMaybe |
| mTreeMap       | Tree-based map/dictionary implementation               | mStd, mAssert, mError, mMaybe, mRef, mStream, mMath, mArrayList                               |
