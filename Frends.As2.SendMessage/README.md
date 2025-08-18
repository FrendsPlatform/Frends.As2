# Frends.As2.SendMessage

Task to send messaged with AS2 protocol

[![SendMessage_build](https://github.com/FrendsPlatform/Frends.As2/actions/workflows/SendMessage_build_and_test_on_main.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.As2/actions/workflows/SendMessage_build_and_test_on_main.yml)
![Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.As2/Frends.As2.SendMessage|main)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

## Installing

You can install the Task via frends UI Task View.

## Building

### Clone a copy of the repository

`git clone https://github.com/FrendsPlatform/Frends.As2.git`

### Build the project

`dotnet build`

### Run tests

**Prerequisites for integration tests:**
- Docker and Docker Compose installed

**Start test services and run tests:**

```bash
# Start the OpenAS2 container
docker-compose -f Frends.As2.SendMessage.Tests/docker/docker-compose.yaml up -d

# Run the tests
dotnet test

# Optional: Stop the container after testing
docker-compose -f Frends.As2.SendMessage.Tests/docker/docker-compose.yaml down

### Create a NuGet package

`dotnet pack --configuration Release`

### Third party licenses

StyleCop.Analyzer version (unmodified version 1.1.118) used to analyze code uses Apache-2.0 license, full text and
source code can be found at https://github.com/DotNetAnalyzers/StyleCopAnalyzers
