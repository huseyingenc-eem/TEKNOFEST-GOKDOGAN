/* global React */
// ============================================================
// MissionPanel - Görev / Waypoint planlama paneli
// ============================================================

const MissionPanel = ({ waypoints, activeWaypoint, onSelect, onAdd, onDelete, onUpload }) => {
  return (
    <div className="hud-panel hud-corners" style={mpStyles.wrap}>
      <span className="corner-bl"/><span className="corner-br"/>
      <div style={mpStyles.header}>
        <span className="hud-label">▸ GÖREV PLANI</span>
        <span className="hud-chip">{waypoints.length} WP</span>
      </div>

      <div style={mpStyles.stats}>
        <StatMini label="MESAFE" value="4.8" unit="km"/>
        <StatMini label="TAHMİNİ" value="12:40" unit="min"/>
        <StatMini label="MAX ALT" value="220" unit="m"/>
      </div>

      <div style={mpStyles.list}>
        {waypoints.map((wp, i) => (
          <div
            key={i}
            onClick={() => onSelect(i)}
            style={{
              ...mpStyles.listItem,
              ...(activeWaypoint === i ? mpStyles.listItemActive : {})
            }}
          >
            <div style={{
              width: 22, height: 22, display: 'grid', placeItems: 'center',
              border: `1.5px solid ${wp.type === 'takeoff' ? 'var(--hud-green)' : wp.type === 'land' ? 'var(--danger)' : 'var(--accent)'}`,
              color: wp.type === 'takeoff' ? 'var(--hud-green)' : wp.type === 'land' ? 'var(--danger-bright)' : 'var(--accent-bright)',
              fontSize: 10, fontFamily: 'var(--font-mono)', fontWeight: 700,
              borderRadius: '50%',
            }}>{i + 1}</div>
            <div style={{flex: 1, minWidth: 0}}>
              <div style={{display: 'flex', gap: 6, alignItems: 'baseline'}}>
                <span className="mono" style={{fontSize: 11, color: '#fff', fontWeight: 600, letterSpacing: '0.05em'}}>
                  {wp.type === 'takeoff' ? 'KALKIŞ' : wp.type === 'land' ? 'İNİŞ' : wp.type === 'loiter' ? 'LOITER' : 'WAYPOINT'}
                </span>
                <span style={{fontSize: 10, color: 'var(--text-secondary)'}}>{wp.alt}m</span>
              </div>
              <div className="mono" style={{fontSize: 9.5, color: 'var(--text-dim)', marginTop: 1}}>
                {wp.lat.toFixed(5)}°N, {wp.lng.toFixed(5)}°E
              </div>
            </div>
            <button
              onClick={(e) => { e.stopPropagation(); onDelete(i); }}
              style={{
                background: 'transparent', border: 'none', color: 'var(--text-dim)',
                cursor: 'pointer', padding: 4, fontSize: 14,
              }}
            >×</button>
          </div>
        ))}

        {waypoints.length === 0 && (
          <div style={{padding: 24, textAlign: 'center', color: 'var(--text-dim)', fontSize: 11, fontFamily: 'var(--font-mono)'}}>
            HARİTAYA TIKLAYARAK<br/>WAYPOINT EKLE
          </div>
        )}
      </div>

      <div style={mpStyles.actions}>
        <button className="hud-btn ghost" onClick={onAdd} style={{flex: 1, padding: '10px 0', fontSize: 11}}>＋ EKLE</button>
        <button className="hud-btn primary" onClick={onUpload} style={{flex: 1.4, padding: '10px 0', fontSize: 11}}>↑ İHA'YA YÜKLE</button>
      </div>
    </div>
  );
};

const StatMini = ({ label, value, unit }) => (
  <div style={{flex: 1, textAlign: 'center', padding: '8px 0', background: 'rgba(255,107,26,0.04)', border: '1px solid var(--border-dim)'}}>
    <div className="hud-label-dim" style={{fontSize: 9}}>{label}</div>
    <div className="display tabular" style={{fontSize: 15, fontWeight: 600, color: '#fff', lineHeight: 1.1}}>
      {value}<span style={{fontSize: 9, color: 'var(--text-secondary)', marginLeft: 2}}>{unit}</span>
    </div>
  </div>
);

const mpStyles = {
  wrap: { width: 300, display: 'flex', flexDirection: 'column', maxHeight: '100%' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 14px 10px', borderBottom: '1px solid var(--border-dim)' },
  stats: { display: 'flex', gap: 4, padding: '10px 12px', borderBottom: '1px solid var(--border-dim)' },
  list: { flex: 1, overflowY: 'auto', padding: '6px 8px', minHeight: 200 },
  listItem: {
    display: 'flex', alignItems: 'center', gap: 10, padding: '8px 6px',
    cursor: 'pointer', borderBottom: '1px solid var(--border-dim)',
    transition: 'background 0.15s',
  },
  listItemActive: {
    background: 'rgba(255,107,26,0.12)',
    borderLeft: '2px solid var(--accent)',
    paddingLeft: 4,
  },
  actions: { display: 'flex', gap: 6, padding: 10, borderTop: '1px solid var(--border-dim)' },
};

window.MissionPanel = MissionPanel;
