const domains = [
  {
    name: 'Registry & Governance',
    key: 'dod.registry',
    status: 'Draft',
    owner: 'Core'
  },
  {
    name: 'Manifest & Capability',
    key: 'dod.manifest',
    status: 'Draft',
    owner: 'Core'
  },
  {
    name: 'Scope Modeling',
    key: 'dod.scope',
    status: 'Draft',
    owner: 'Core'
  },
  {
    name: 'Knowledge Assets',
    key: 'dod.assets',
    status: 'Draft',
    owner: 'Core'
  }
]

const workflows = [
  {
    name: 'Domain Activation',
    nodes: 6,
    state: 'Idle'
  },
  {
    name: 'Asset Ingestion',
    nodes: 8,
    state: 'Queued'
  },
  {
    name: 'Manifest Publish',
    nodes: 5,
    state: 'Idle'
  }
]

const assets = [
  {
    type: 'Plan',
    title: 'DoD Domains + Workflows v2',
    scope: 'tenant:local/domain:dod'
  },
  {
    type: 'Schema',
    title: 'DomainManifest v1',
    scope: 'tenant:local/domain:dod/manifest'
  },
  {
    type: 'Decision',
    title: 'RavenDB + split Postgres',
    scope: 'tenant:local/domain:dod/policy'
  }
]

export default function App() {
  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark">DoD</span>
          <div>
            <div className="brand-title">Cognition Console</div>
            <div className="brand-subtitle">Domain of Domains control plane</div>
          </div>
        </div>
        <div className="topbar-actions">
          <button className="ghost">Sync artifacts</button>
          <button className="primary">New domain</button>
        </div>
      </header>

      <main className="shell">
        <section className="hero">
          <div>
            <p className="kicker">Governed context, continuous learning</p>
            <h1>
              Shape the knowledge boundaries before the model ever speaks.
            </h1>
            <p className="hero-copy">
              DoD is the registry for every domain, policy, scope, and workflow. Keep it
              explicit, auditable, and calm under load.
            </p>
            <div className="hero-actions">
              <button className="primary">Create domain</button>
              <button className="ghost">Review policies</button>
            </div>
          </div>
          <div className="hero-panel">
            <div className="panel-title">System posture</div>
            <div className="panel-grid">
              <div>
                <div className="metric">4</div>
                <div className="metric-label">Domains drafting</div>
              </div>
              <div>
                <div className="metric">2</div>
                <div className="metric-label">Workflows queued</div>
              </div>
              <div>
                <div className="metric">18</div>
                <div className="metric-label">Policies active</div>
              </div>
              <div>
                <div className="metric">0</div>
                <div className="metric-label">Critical alerts</div>
              </div>
            </div>
            <div className="panel-foot">
              Last indexed: <span>02:14 UTC</span>
            </div>
          </div>
        </section>

        <section className="grid">
          <div className="card">
            <div className="card-header">
              <h2>Domains in flight</h2>
              <span className="pill">Draft</span>
            </div>
            <div className="card-body">
              {domains.map((domain) => (
                <div key={domain.key} className="row">
                  <div>
                    <div className="row-title">{domain.name}</div>
                    <div className="row-sub">{domain.key}</div>
                  </div>
                  <div className="row-meta">
                    <span>{domain.owner}</span>
                    <span className="tag">{domain.status}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <h2>Workflow lanes</h2>
              <span className="pill">Queued</span>
            </div>
            <div className="card-body">
              {workflows.map((workflow) => (
                <div key={workflow.name} className="row">
                  <div>
                    <div className="row-title">{workflow.name}</div>
                    <div className="row-sub">{workflow.nodes} nodes</div>
                  </div>
                  <div className="row-meta">
                    <span className="tag muted">{workflow.state}</span>
                  </div>
                </div>
              ))}
              <div className="lane-hint">
                Graph engine will connect here once nodes are registered.
              </div>
            </div>
          </div>
        </section>

        <section className="grid wide">
          <div className="card wide">
            <div className="card-header">
              <h2>Recent assets</h2>
              <span className="pill">Scoped</span>
            </div>
            <div className="card-body">
              <div className="asset-grid">
                {assets.map((asset) => (
                  <div key={asset.title} className="asset">
                    <div className="asset-type">{asset.type}</div>
                    <div className="asset-title">{asset.title}</div>
                    <div className="asset-scope">{asset.scope}</div>
                  </div>
                ))}
              </div>
            </div>
          </div>
          <div className="card wide">
            <div className="card-header">
              <h2>Signals</h2>
              <span className="pill">Live</span>
            </div>
            <div className="card-body">
              <div className="signal">
                <div className="signal-title">Event bus</div>
                <div className="signal-detail">Listening for DoD event types (3 registered).</div>
              </div>
              <div className="signal">
                <div className="signal-title">Hub status</div>
                <div className="signal-detail">Conversation flow issues pending triage.</div>
              </div>
              <div className="signal">
                <div className="signal-title">RavenDB</div>
                <div className="signal-detail">Document store idle. Awaiting first manifest publish.</div>
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer className="footer">
        <span>Scope guarded. Actions gated. Everything audited.</span>
        <span>v0.1 DoD console</span>
      </footer>
    </div>
  )
}
