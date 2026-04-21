/* global React */
// ============================================================
// TelemetryPanel - Sol alt telemetri kutusu
// Batarya, GPS, sinyal, motor, sıcaklık
// ============================================================

const TelemetryBar = ({ label, value, unit, pct, color = 'var(--accent)', warning }) => (
  <div style={{marginBottom: 10}}>
    <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: 3}}>
      <span className="hud-label-dim">{label}</span>
      <span className="mono tabular" style={{fontSize: 12, color: warning ? 'var(--danger-bright)' : '#fff', fontWeight: 600}}>
        {value}<span style={{opacity: 0.5, marginLeft: 2}}>{unit}</span>
      </span>
    </div>
    <div style={{height: 3, background: 'rgba(255,255,255,0.06)', position: 'relative'}}>
      <div style={{
        position: 'absolute', left: 0, top: 0, bottom: 0,
        width: `${pct}%`, background: color,
        boxShadow: `0 0 6px ${color}`,
        transition: 'width 0.5s ease',
      }}/>
    </div>
  </div>
);

const TelemetryPanel = ({ telemetry }) => {
  return (
    <div className="hud-panel hud-corners" style={telStyles.wrap}>
      <span className="corner-bl"/><span className="corner-br"/>
      <div style={telStyles.header}>
        <span className="hud-label">▸ TELEMETRİ</span>
        <span className="hud-chip green">LIVE</span>
      </div>

      <div style={telStyles.body}>
        <TelemetryBar
          label="BATARYA"
          value={telemetry.battery.toFixed(1)} unit="V"
          pct={(telemetry.battery - 18) / (25.2 - 18) * 100}
          color={telemetry.battery > 22 ? 'var(--hud-green)' : telemetry.battery > 20 ? 'var(--warn)' : 'var(--danger)'}
          warning={telemetry.battery < 20}
        />
        <TelemetryBar
          label="SİNYAL RSSI"
          value={telemetry.rssi} unit="dBm"
          pct={Math.min(100, (telemetry.rssi + 100) * 2)}
          color="var(--accent)"
        />
        <TelemetryBar
          label="GPS HDOP"
          value={telemetry.hdop.toFixed(2)} unit=""
          pct={Math.max(0, 100 - telemetry.hdop * 30)}
          color="var(--hud-green)"
        />
        <TelemetryBar
          label="MOTOR"
          value={telemetry.throttle} unit="%"
          pct={telemetry.throttle}
          color="var(--accent)"
        />

        <div style={telStyles.grid}>
          <MetricTile label="UYDU" value={telemetry.satellites} icon="◈"/>
          <MetricTile label="SICAKLIK" value={`${telemetry.temp}°`} icon="🌡" hideIcon/>
          <MetricTile label="AKIM" value={`${telemetry.current.toFixed(1)}A`}/>
          <MetricTile label="MAH" value={telemetry.mah}/>
        </div>
      </div>
    </div>
  );
};

const MetricTile = ({ label, value }) => (
  <div style={telStyles.tile}>
    <div className="hud-label-dim" style={{fontSize: 9}}>{label}</div>
    <div className="display tabular" style={{fontSize: 16, fontWeight: 600, color: '#fff', lineHeight: 1.1}}>{value}</div>
  </div>
);

const telStyles = {
  wrap: { width: 260, padding: 14 },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 14, paddingBottom: 8, borderBottom: '1px solid var(--border-dim)' },
  body: {},
  grid: { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6, marginTop: 10 },
  tile: { background: 'rgba(255,255,255,0.02)', border: '1px solid var(--border-dim)', padding: '6px 8px' },
};

window.TelemetryPanel = TelemetryPanel;
window.TelemetryBar = TelemetryBar;
window.MetricTile = MetricTile;
