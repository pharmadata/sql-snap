# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [1.1.0] - 2017-05-15
### Added
- Ability to backup/restore multiple databases in one snapshot

### Changed
- **Breaking change:** The metadata argument now specifies a directory, not a file.
  This is required to support multiple databases in a single snapshot.  The metadata will
  now be written as DatabaseName.metadata to the directory specified.

## [1.0.0] - First release
