# SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
# SPDX-License-Identifier: LGPL-3.0-only

FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy AS build
COPY . .
RUN dotnet publish ./tools/SendBlobs/SendBlobs.csproj -c release --sc false -o out -p:BuildTimestamp=0


FROM mcr.microsoft.com/dotnet/runtime:7.0
RUN apt-get update && apt-get -y install libsnappy-dev
WORKDIR /nethermind
COPY --from=build /out/SendBlobs .
ENTRYPOINT ["./SendBlobs"]
