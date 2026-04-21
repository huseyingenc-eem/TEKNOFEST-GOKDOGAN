/* global React */
// ============================================================
// AlertSystem - Sağ üstten giren toast uyarıları
// Jüri kurtarıcısı: HSS yaklaşma, sınır yaklaşma, batarya kritik,
// haberleşme gecikme, kilitlenme tamamlandı
// ============================================================

const { useState, useEffect, useCallback } = React;

const ALERT_PRESETS = {
  hss:       { level: 'danger',  icon: '⏣',  title: 'HSS YAKLAŞMA',       sound: 'BEEP' },
  border:    { level: 'warn',    icon: '⛌',  title: 'SINIR YAKLAŞMA',     sound: 'BEEP' },
  battery:   { level: 'danger',  icon: '▼',  title: 'BATARYA KRİTİK',     sound: 'BEEP' },
  comms:     { level: 'warn',    icon: '⇅',  title: 'HABERLEŞME GECİKME', sound: '' },
  lock:      { level: 'success', icon: '◎',  title: 'KİLİTLENME TAMAM',   sound: 'LOCK' },
  kamikaze:  { level: 'danger',  icon: '✕',  title: 'KAMİKAZE GÖREV',     sound: 'BEEP' },
  geofence:  { level: 'warn',    icon: '⬠',  title: 'UÇUŞA YASAK BÖLGE',  sound: '' },
};

function useAlertQueue() {
  const [alerts, setAlerts] = useState([]);
  const push = useCallback((kind, message, ttl = 5000) => {
    const id = Date.now() + Math.random();
    const preset = ALERT_PRESETS[kind] || ALERT_PRESETS.comms;
    setAlerts(a => [...a, { id, kind, preset, message }]);
    setTimeout(() => setAlerts(a => a.filter(x => x.id !== id)), ttl);
  }, []);
  return { alerts, push };
}

const AlertToast = ({ alert }) => {
  const { preset, message } = alert;
  const color = preset.level === 'danger' ? 'var(--danger)'
              : preset.level === 'warn'   ? 'var(--warn)'
              : 'var(--hud-green)';
  const bright = preset.level === 'danger' ? 'var(--danger-bright)'
              : preset.level === 'warn'   ? 'var(--warn)'
              : 'var(--hud-green)';

  return (
    <div style={{
      background: 'rgba(14,18,24,0.95)',
      border: `1px solid ${color}`,
      borderLeft: `4px solid ${color}`,
      padding: '10px 14px',
      minWidth: 280, maxWidth: 340,
      display: 'flex', gap: 10, alignItems: 'center',
      animation: 'alertSlide 0.3s ease-out',
      boxShadow: `0 0 14px ${color}55`,
    }}>
      <style>{`
        @keyframes alertSlide {
          from { transform: translateX(30px); opacity: 0; }
          to { transform: translateX(0); opacity: 1; }
        }
      `}</style>
      <div style={{
        width: 32, height: 32, display: 'grid', placeItems: 'center',
        border: `1.5px solid ${color}`, color: bright, fontSize: 18, fontWeight: 700,
      }} className={preset.level === 'danger' ? 'blink' : ''}>{preset.icon}</div>
      <div style={{flex: 1}}>
        <div className="display" style={{fontSize: 12, fontWeight: 700, color: bright, letterSpacing: '0.12em'}}>
          {preset.title}
        </div>
        <div className="mono" style={{fontSize: 11, color: '#e8ecf1', marginTop: 2}}>
          {message}
        </div>
      </div>
    </div>
  );
};

const AlertStack = ({ alerts, compact }) => (
  <div style={{
    position: 'absolute',
    top: compact ? 56 : 12, right: compact ? 12 : 16,
    display: 'flex', flexDirection: 'column', gap: 8,
    zIndex: 200, pointerEvents: 'none',
  }}>
    {alerts.map(a => <AlertToast key={a.id} alert={a}/>)}
  </div>
);

window.useAlertQueue = useAlertQueue;
window.AlertStack = AlertStack;
window.ALERT_PRESETS = ALERT_PRESETS;
