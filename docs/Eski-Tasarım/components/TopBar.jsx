/* global React */
// ============================================================
// TopBar - Üst bilgi çubuğu
// Takım adı, görev durumu, saat, bağlantı durumu, ana sekmeler
// ============================================================

const TopBar = ({ activeView, onViewChange, missionTime, connection }) => {
  const views = [
    { id: 'map', label: 'HARİTA', icon: '⊕' },
    { id: 'camera', label: 'KAMERA', icon: '◉' },
    { id: 'mission', label: 'GÖREV', icon: '⟲' },
  ];

  return (
    <div style={topBarStyles.wrap}>
      {/* Sol: Logo + Takım */}
      <div style={topBarStyles.left}>
        <div style={topBarStyles.logo}>
          <svg width="28" height="28" viewBox="0 0 32 32" fill="none">
            <path d="M16 3 L29 26 L16 20 L3 26 Z" stroke="#ff6b1a" strokeWidth="1.5" fill="rgba(255,107,26,0.15)"/>
            <circle cx="16" cy="18" r="2" fill="#ff6b1a"/>
          </svg>
        </div>
        <div style={{display: 'flex', flexDirection: 'column', lineHeight: 1.1}}>
          <span className="display" style={{fontSize: 17, fontWeight: 700, letterSpacing: '0.15em', color: '#fff'}}>GÖKDOĞAN</span>
          <span className="mono" style={{fontSize: 9, color: 'var(--accent)', letterSpacing: '0.2em'}}>TEKNOFEST · SAVAŞAN İHA</span>
        </div>
      </div>

      {/* Orta: Görünüm sekmeleri */}
      <div style={topBarStyles.tabs}>
        {views.map(v => (
          <button
            key={v.id}
            onClick={() => onViewChange(v.id)}
            style={{
              ...topBarStyles.tab,
              ...(activeView === v.id ? topBarStyles.tabActive : {})
            }}
          >
            <span style={{opacity: 0.7, marginRight: 6}}>{v.icon}</span>
            {v.label}
          </button>
        ))}
      </div>

      {/* Sağ: Durum göstergeleri */}
      <div style={topBarStyles.right}>
        <div style={topBarStyles.statItem}>
          <span className="hud-label-dim">MISSION</span>
          <span className="mono tabular" style={{color: 'var(--accent-bright)', fontSize: 14, fontWeight: 600}}>
            {missionTime}
          </span>
        </div>
        <div style={topBarStyles.divider}/>
        <div style={topBarStyles.statItem}>
          <span className="hud-label-dim">LINK</span>
          <div className="row gap-2">
            <span className="pulse" style={{
              width: 8, height: 8, borderRadius: '50%',
              background: connection.quality > 70 ? 'var(--hud-green)' : connection.quality > 30 ? 'var(--warn)' : 'var(--danger)',
              boxShadow: `0 0 8px currentColor`
            }}/>
            <span className="mono tabular" style={{fontSize: 13, color: '#fff'}}>{connection.quality}%</span>
          </div>
        </div>
        <div style={topBarStyles.divider}/>
        <div style={topBarStyles.statItem}>
          <span className="hud-label-dim">UTC</span>
          <span className="mono tabular" style={{fontSize: 13, color: '#fff'}}>{connection.utc}</span>
        </div>
      </div>
    </div>
  );
};

const topBarStyles = {
  wrap: {
    height: 56,
    background: 'linear-gradient(180deg, #151a22 0%, #0d1117 100%)',
    borderBottom: '1px solid var(--border)',
    display: 'flex',
    alignItems: 'center',
    padding: '0 16px',
    gap: 24,
    position: 'relative',
    zIndex: 100,
  },
  left: { display: 'flex', alignItems: 'center', gap: 12, minWidth: 260 },
  logo: {
    width: 40, height: 40,
    display: 'grid', placeItems: 'center',
    background: 'rgba(255, 107, 26, 0.08)',
    border: '1px solid var(--border-strong)',
  },
  tabs: { display: 'flex', gap: 2, flex: 1, justifyContent: 'center' },
  tab: {
    fontFamily: 'var(--font-display)', fontSize: 13, fontWeight: 600,
    letterSpacing: '0.15em', textTransform: 'uppercase',
    background: 'transparent', border: '1px solid transparent',
    borderBottom: '2px solid transparent',
    color: 'var(--text-secondary)', padding: '8px 24px',
    cursor: 'pointer', transition: 'all 0.15s',
  },
  tabActive: {
    color: 'var(--accent-bright)',
    borderBottomColor: 'var(--accent)',
    background: 'rgba(255, 107, 26, 0.08)',
    textShadow: '0 0 8px rgba(255, 107, 26, 0.5)',
  },
  right: { display: 'flex', alignItems: 'center', gap: 16, minWidth: 320, justifyContent: 'flex-end' },
  statItem: { display: 'flex', flexDirection: 'column', lineHeight: 1.2, gap: 2 },
  divider: { width: 1, height: 28, background: 'var(--border-dim)' },
};

window.TopBar = TopBar;
