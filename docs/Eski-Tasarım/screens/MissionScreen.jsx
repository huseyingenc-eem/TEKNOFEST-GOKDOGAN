/* global React */
// ============================================================
// MissionScreen - GÖREV planlama ekranı (Varyasyon 3)
// Büyük harita + waypoint listesi + görev parametreleri
// ============================================================

const MissionScreen = () => {
  const sim = window.useSimulation();
  const [mapType, setMapType] = React.useState('topo');
  const [activeWp, setActiveWp] = React.useState(2);
  const [layers, setLayers] = React.useState({
    own: true, friendly: true, hostile: true, waypoints: true, trail: false, noFly: true,
  });

  const handleMapClick = (e) => {
    // Mock: tıklanan yere waypoint ekle
    const rect = e.currentTarget.getBoundingClientRect();
    const x = (e.clientX - rect.left) / rect.width;
    const y = (e.clientY - rect.top) / rect.height;
    // Inverse project
    const centerLat = 41.0, centerLng = 29.0, scale = 0.08;
    const lng = centerLng + (x * 2 - 1) * scale;
    const lat = centerLat - (y * 2 - 1) * scale;
    sim.setWaypoints(wps => {
      const newWps = [...wps];
      // İniş noktasından önce ekle
      newWps.splice(newWps.length - 1, 0, {
        type: 'waypoint', lat, lng, alt: 180,
      });
      return newWps;
    });
    sim.addLog('success', 'Waypoint eklendi');
  };

  const handleDelete = (i) => {
    sim.setWaypoints(wps => wps.filter((_, idx) => idx !== i));
    sim.addLog('info', `Waypoint ${i + 1} silindi`);
  };

  const handleUpload = () => {
    sim.addLog('success', `${sim.waypoints.length} waypoint İHA'ya yüklendi`);
  };

  return (
    <div style={{position: 'absolute', inset: 0, display: 'flex'}}>
      {/* Sol panel - Mission */}
      <div style={{
        width: 320, flexShrink: 0,
        background: 'var(--bg-secondary)',
        borderRight: '1px solid var(--border)',
        padding: 16, display: 'flex', flexDirection: 'column', gap: 12,
      }}>
        <window.MissionPanel
          waypoints={sim.waypoints}
          activeWaypoint={activeWp}
          onSelect={setActiveWp}
          onAdd={() => sim.addLog('info', 'Haritaya tıklayın')}
          onDelete={handleDelete}
          onUpload={handleUpload}
        />

        {/* Görev parametreleri */}
        <div className="hud-panel hud-corners" style={{padding: 12}}>
          <span className="corner-bl"/><span className="corner-br"/>
          <div className="hud-label" style={{marginBottom: 10}}>▸ PARAMETRELER</div>
          <ParamRow label="VARSAYILAN HIZ" value="24" unit="m/s"/>
          <ParamRow label="KALKIŞ İRTİFASI" value="50" unit="m"/>
          <ParamRow label="GÖREV İRTİFASI" value="180" unit="m"/>
          <ParamRow label="LOITER YARIÇAPI" value="60" unit="m"/>
          <ParamRow label="RTL İRTİFASI" value="120" unit="m"/>
          <ParamRow label="MAX EĞİM" value="35" unit="°"/>
        </div>
      </div>

      {/* Harita */}
      <div style={{flex: 1, position: 'relative'}}>
        <window.MapView
          mapType={mapType}
          ownAircraft={sim.ownAircraft}
          otherAircraft={sim.otherAircraft}
          waypoints={sim.waypoints}
          onMapClick={handleMapClick}
        />

        <window.MapControls
          mapType={mapType}
          onMapTypeChange={setMapType}
          layers={layers}
          onLayerToggle={(k) => setLayers(p => ({...p, [k]: !p[k]}))}
          onZoom={() => {}}
        />

        {/* Orta üst - mod göstergesi */}
        <div style={{
          position: 'absolute', top: 16, left: '50%', transform: 'translateX(-50%)',
          zIndex: 20,
        }}>
          <div className="hud-panel" style={{
            padding: '8px 20px', display: 'flex', gap: 24, alignItems: 'center',
            border: '1px solid var(--accent)',
          }}>
            <span className="display" style={{fontSize: 14, color: 'var(--accent-bright)', letterSpacing: '0.2em', fontWeight: 700}}>
              ▸ GÖREV PLANLAMA MODU
            </span>
            <span className="mono" style={{fontSize: 10, color: 'var(--text-secondary)'}}>
              HARİTAYA TIKLAYARAK WAYPOINT EKLE · SAĞ TIK SİLER
            </span>
          </div>
        </div>

        {/* Seçili waypoint bilgisi */}
        {sim.waypoints[activeWp] && (
          <div style={{
            position: 'absolute', bottom: 16, left: 16, zIndex: 20,
            width: 300,
          }}>
            <div className="hud-panel hud-corners" style={{padding: 14}}>
              <span className="corner-bl"/><span className="corner-br"/>
              <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: 10}}>
                <span className="hud-label">▸ WP-{activeWp + 1} DETAYI</span>
                <span className="hud-chip">{sim.waypoints[activeWp].type?.toUpperCase()}</span>
              </div>
              <div style={{display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '6px 14px', fontFamily: 'var(--font-mono)', fontSize: 11}}>
                <span style={{color: 'var(--text-dim)'}}>LAT</span>
                <span style={{color: '#fff'}}>{sim.waypoints[activeWp].lat.toFixed(6)}°N</span>
                <span style={{color: 'var(--text-dim)'}}>LNG</span>
                <span style={{color: '#fff'}}>{sim.waypoints[activeWp].lng.toFixed(6)}°E</span>
                <span style={{color: 'var(--text-dim)'}}>İRTİFA</span>
                <span style={{color: 'var(--accent-bright)'}}>{sim.waypoints[activeWp].alt} m AGL</span>
                <span style={{color: 'var(--text-dim)'}}>MESAFE</span>
                <span style={{color: '#fff'}}>~650 m</span>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

const ParamRow = ({ label, value, unit }) => (
  <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', padding: '5px 0', borderBottom: '1px solid var(--border-dim)'}}>
    <span className="hud-label-dim" style={{fontSize: 10}}>{label}</span>
    <span className="mono tabular" style={{color: '#fff', fontSize: 12}}>
      {value}<span style={{color: 'var(--text-secondary)', marginLeft: 3, fontSize: 10}}>{unit}</span>
    </span>
  </div>
);

window.MissionScreen = MissionScreen;
