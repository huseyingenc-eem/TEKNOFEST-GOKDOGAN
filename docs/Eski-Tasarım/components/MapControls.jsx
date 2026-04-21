/* global React */
// ============================================================
// MapControls - Harita üstü butonlar (zoom, tip değiştir, katmanlar)
// ============================================================

const MapControls = ({ mapType, onMapTypeChange, layers, onLayerToggle, onZoom }) => {
  return (
    <>
      {/* Sağ üst - harita tipi */}
      <div style={mcStyles.typeSwitch}>
        <button
          className="hud-btn ghost"
          onClick={() => onMapTypeChange('satellite')}
          style={{
            padding: '6px 12px', fontSize: 11,
            ...(mapType === 'satellite' ? {background: 'rgba(255,107,26,0.2)', color: 'var(--accent-bright)', borderColor: 'var(--accent)'} : {})
          }}
        >UYDU</button>
        <button
          className="hud-btn ghost"
          onClick={() => onMapTypeChange('topo')}
          style={{
            padding: '6px 12px', fontSize: 11,
            ...(mapType === 'topo' ? {background: 'rgba(255,107,26,0.2)', color: 'var(--accent-bright)', borderColor: 'var(--accent)'} : {})
          }}
        >TOPO</button>
      </div>

      {/* Zoom kontrolleri */}
      <div style={mcStyles.zoomStack}>
        <button style={mcStyles.iconBtn} onClick={() => onZoom(1)}>＋</button>
        <button style={mcStyles.iconBtn} onClick={() => onZoom(-1)}>−</button>
        <button style={mcStyles.iconBtn} title="Kendine merkezle">◉</button>
      </div>

      {/* Katman switcher */}
      <div style={mcStyles.layerPanel} className="hud-panel hud-corners">
        <span className="corner-bl"/><span className="corner-br"/>
        <div className="hud-label" style={{marginBottom: 8, fontSize: 9}}>▸ KATMANLAR</div>
        {Object.entries(layers).map(([key, val]) => (
          <label key={key} style={mcStyles.layerItem}>
            <span style={{
              width: 12, height: 12,
              border: '1px solid var(--border-strong)',
              background: val ? 'var(--accent)' : 'transparent',
              display: 'inline-block',
            }}/>
            <input type="checkbox" checked={val} onChange={() => onLayerToggle(key)} style={{display: 'none'}}/>
            <span style={{fontSize: 10, color: val ? '#fff' : 'var(--text-secondary)', fontFamily: 'var(--font-mono)', textTransform: 'uppercase', letterSpacing: '0.08em'}}>
              {key === 'own' ? 'KENDİMİZ' : key === 'friendly' ? 'DOST (YEŞİL)' : key === 'hostile' ? 'HASIM (KIRMIZI)' : key === 'waypoints' ? 'WAYPOINT' : key === 'trail' ? 'İZ' : key === 'borders' ? 'SINIR · HSS' : key === 'noFly' ? 'UÇUŞA YASAK' : key}
            </span>
          </label>
        ))}
      </div>
    </>
  );
};

const mcStyles = {
  typeSwitch: {
    position: 'absolute', top: 16, right: 16, display: 'flex', gap: 4, zIndex: 20,
  },
  zoomStack: {
    position: 'absolute', right: 16, top: 74, display: 'flex', flexDirection: 'column', gap: 4, zIndex: 20,
  },
  iconBtn: {
    width: 36, height: 36,
    background: 'rgba(14, 18, 24, 0.82)',
    border: '1px solid var(--border-strong)',
    color: 'var(--accent-bright)',
    fontSize: 18, fontWeight: 700, cursor: 'pointer',
    backdropFilter: 'blur(8px)',
    transition: 'all 0.15s',
  },
  layerPanel: {
    position: 'absolute', right: 16, top: 210, zIndex: 20,
    padding: '10px 12px', width: 170,
  },
  layerItem: {
    display: 'flex', alignItems: 'center', gap: 8, padding: '4px 0',
    cursor: 'pointer',
  },
};

window.MapControls = MapControls;
