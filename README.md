# BGPLite

Lightweight BGP route server with community-based filtering and HTTP management API.

Built with .NET 10, designed for scenarios where you need to accept BGP sessions from peers, store routes, and control which routes each peer receives based on BGP communities.

## Features

- BGP session management (OPEN, UPDATE, KEEPALIVE, NOTIFICATION)
- 4-byte ASN support
- Route table with prefix storage and community tagging
- Per-peer community-based route filtering
- HTTP management API for runtime configuration
- SQLite peer store
- Docker support

## Requirements

- .NET 10.0 SDK
- Linux (BGP port 179 requires root or `CAP_NET_BIND_SERVICE`)

## Quick Start

Copy the example config and edit with your settings:

```bash
cp appsettings.Example.yml appsettings.yml
```

Run:

```bash
sudo dotnet run --project BGPLite
```

Or with Docker:

```bash
docker build -t bgplite .
docker run -d \
  -p 179:179 \
  -p 5000:5000 \
  -v $(pwd)/appsettings.yml:/app/appsettings.yml \
  bgplite
```

## Configuration

```yaml
Bgp:
  Asn: 65444
  RouterId: 10.0.0.1
  KeepAlive: 60
  HoldTime: 180

Peers:
  - Address: 10.0.0.2
    RemoteAsn: 65001
    Description: "example-peer"
```

The `Peers` section is optional — unconfigured peers can connect dynamically.

### Route Data

Place prefix files in the working directory:

- `nets.txt` — default routes (one prefix per line, e.g. `203.0.113.0/24`)
- `communities/` — community-tagged routes, files named `{ASN}_{value}.txt`

### Data Directory

Peer data is stored in SQLite at `$BGPLITE_DATA/bgplite.db` (defaults to `./data`).

## Management API

Available on port 5000:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/peers` | List all connected peers |
| GET | `/api/routes/count` | Route counts by community |
| GET | `/api/peer/{ip}/communities` | Get peer community filter |
| PUT | `/api/peer/{ip}/communities` | Set peer community filter |
| DELETE | `/api/peer/{ip}/communities` | Clear community filter (accept all routes) |
| PUT | `/api/peer/{ip}/description` | Set peer description |

### Examples

```bash
# List peers
curl http://localhost:5000/api/peers

# Set community filter for a peer
curl -X PUT http://localhost:5000/api/peer/10.0.0.2/communities \
  -H 'Content-Type: application/json' \
  -d '{"communities": ["65444:100", "65444:200"]}'

# Route statistics
curl http://localhost:5000/api/routes/count
```

## Project Structure

```
BGPLite/
├── BGPLite/              # Entry point, host setup
├── BGPLite.Protocol/     # BGP message encoding/decoding
├── BGPLite.Server/       # TCP listener, BGP session FSM
├── BGPLite.Routing/      # Route table, community filters
├── BGPLite.Configuration/ # YAML config loading
├── BGPLite.Api/          # Management HTTP API, peer store
└── BGPLite.Tests/        # Unit tests
```

## License

MIT
