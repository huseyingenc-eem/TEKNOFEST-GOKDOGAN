/* global React */
// ============================================================
// LogConsole - Uyarı/mesaj konsolu (alt)
// ============================================================

const LogConsole = ({ logs }) => {
  const levelColor = {
    info: 'var(--accent-bright)',
    warn: 'var(--warn)',
    error: 'var(--danger-bright)',
    success: 'var(--hud-green)',
  };

  return (
    <div className="hud-panel" style={lcStyles.wrap}>
      <div style={lcStyles.header}>
        <span className="hud-label">▸ SİSTEM LOGU</span>
        <span className="hud-chip">{logs.length}</span>
      </div>
      <div style={lcStyles.body}>
        {logs.slice(-4).reverse().map((log, i) => (
          <div key={i} style={{display: 'flex', gap: 10, alignItems: 'baseline', padding: '3px 0', fontFamily: 'var(--font-mono)', fontSize: 11}}>
            <span style={{color: 'var(--text-dim)', minWidth: 62}}>{log.time}</span>
            <span style={{
              color: levelColor[log.level],
              minWidth: 50, textTransform: 'uppercase', fontSize: 9.5, letterSpacing: '0.1em',
            }}>[{log.level}]</span>
            <span style={{color: '#d5dae3', flex: 1}}>{log.msg}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

const lcStyles = {
  wrap: { padding: 0 },
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '6px 12px', borderBottom: '1px solid var(--border-dim)',
  },
  body: { padding: '6px 12px', maxHeight: 100, overflowY: 'auto' },
};

window.LogConsole = LogConsole;
