/* global React */
// ============================================================
// MapScreen v2 - Sol sabit sidebar + büyük harita + sağ ince rail
// Harita artık gerçekten büyük ve oynatılabilir
// ============================================================

const MapScreen = () => {
  const sim = window.useSimulation();
  const [mapType, setMapType] = React.useState('satellite');
  const [layers, setLayers] = React.useState({
    own: true, friendly: true, hostile: true, waypoints: true, trail: true, borders: true,
  });
  const [rightTab, setRightTab] = React.useState('contacts');

  return (
    <div style={{position: 'absolute', inset: 0, display: 'flex'}}>
      {/* ========== SOL SIDEBAR (280px sabit) ========== */}
      <div style={{
        width: 280, flexShrink: 0, background: 'var(--bg-secondary)',
        borderRight: '1px solid var(--border)',
        display: 'flex', flexDirection: 'column', gap: 10, padding: 10,
        overflowY: 'auto',
      }}>
        <window.PFD flight={sim.flight} compact/>
        <window.TelemetryPanel telemetry={sim.telemetry}/>
        <window.EmergencyControls state={sim.state} onAction={sim.handleAction}/>
      </div>

      {/* ========== ORTA: BÜYÜK HARİTA ========== */}
      <div style={{flex: 1, position: 'relative', minWidth: 0}}>
        <window.MapView
          mapType={mapType}
          ownAircraft={sim.ownAircraft}
          otherAircraft={sim.otherAircraft.filter(a =>
            (a.team === 'friendly' && layers.friendly) ||
            (a.team === 'hostile' && layers.hostile) ||
            (a.team === 'unknown' && layers.hostile)
          )}
          waypoints={layers.waypoints ? sim.waypoints : []}
          onMapClick={() => sim.addLog('info', 'Haritada nokta seçildi')}
        />
        <window.BorderOverlay show={layers.borders}/>

        {/* Harita kontrolleri (sağ üst) */}
        <window.MapControls
          mapType={mapType}
          onMapTypeChange={setMapType}
          layers={layers}
          onLayerToggle={(k) => setLayers(p => ({...p, [k]: !p[k]}))}
          onZoom={() => {}}
        />

        {/* Üst orta - Sunucu saati + API durumu */}
        <div style={{
          position: 'absolute', top: 16, left: '50%', transform: 'translateX(-50%)',
          zIndex: 20, display: 'flex', gap: 8,
        }}>
          <div className="hud-panel" style={{padding: '6px 14px', display: 'flex', gap: 14, alignItems: 'center'}}>
            <span className="hud-label-dim" style={{fontSize: 9}}>SUNUCU</span>
            <span className="mono tabular" style={{color: 'var(--hud-green)', fontSize: 12, fontWeight: 600}}>
              {sim.connection.utc}
            </span>
            <div style={{width: 1, height: 14, background: 'var(--border-dim)'}}/>
            <span className="hud-label-dim" style={{fontSize: 9}}>TELEMETRİ</span>
            <span className="mono" style={{color: 'var(--accent-bright)', fontSize: 11}}>1.0 Hz</span>
          </div>
        </div>

        {/* Alt - Log konsolu */}
        <div style={{position: 'absolute', bottom: 12, left: 12, right: 12, zIndex: 20}}>
          <window.LogConsole logs={sim.logs}/>
        </div>

        {/* Toast uyarılar */}
        <window.AlertStack alerts={sim.alerts}/>
      </div>

      {/* ========== SAĞ RAIL (320px) ========== */}
      <div style={{
        width: 320, flexShrink: 0, background: 'var(--bg-secondary)',
        borderLeft: '1px solid var(--border)',
        display: 'flex', flexDirection: 'column',
      }}>
        {/* Sekmeler */}
        <div style={{display: 'flex', borderBottom: '1px solid var(--border)'}}>
          {[
            {id: 'contacts', label: 'HAVA RESMİ'},
            {id: 'hss', label: 'HSS · QR'},
            {id: 'legend', label: 'LEGEND'},
          ].map(t => (
            <button key={t.id} onClick={() => setRightTab(t.id)} style={{
              flex: 1, background: rightTab === t.id ? 'rgba(255,140,66,0.1)' : 'transparent',
              color: rightTab === t.id ? 'var(--accent-bright)' : 'var(--text-secondary)',
              border: 'none', borderBottom: rightTab === t.id ? '2px solid var(--accent)' : '2px solid transparent',
              padding: '10px 0', fontFamily: 'var(--font-display)', fontSize: 11,
              letterSpacing: '0.15em', cursor: 'pointer',
            }}>{t.label}</button>
          ))}
        </div>

        <div style={{flex: 1, overflowY: 'auto', padding: 10}}>
          {rightTab === 'contacts' && (
            <window.ContactList
              contacts={sim.otherAircraft}
              onLock={sim.setLockedId}
              lockedId={sim.lockedId}
            />
          )}
          {rightTab === 'hss' && <window.HSSPanel sim={sim}/>}
          {rightTab === 'legend' && <window.BorderLegend/>}
        </div>
      </div>
    </div>
  );
};

window.MapScreen = MapScreen;
