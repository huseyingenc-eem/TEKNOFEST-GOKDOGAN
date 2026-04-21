/* global React */
// ============================================================
// useSimulation - Canlı telemetri simülasyonu hook'u
// Bütün ekranlar için ortak state, TEKNOFEST API mock'u dahil
// ============================================================

const { useState, useEffect, useRef, useCallback } = React;

function useSimulation() {
  const [tick, setTick] = useState(0);
  const [armed, setArmed] = useState(true);
  const [mode, setMode] = useState('AUTO');
  const [lockedId, setLockedId] = useState('HSM-047');
  const [logs, setLogs] = useState([
    { time: '14:32:08', level: 'info', msg: 'Sistem başlatıldı · GÖKDOĞAN-01 bağlı' },
    { time: '14:32:15', level: 'success', msg: 'GPS fix · 14 uydu · HDOP 0.87' },
    { time: '14:32:44', level: 'info', msg: 'Görev yüklendi · 7 waypoint · 4.8km' },
    { time: '14:33:02', level: 'warn', msg: 'Hasım İHA tespit edildi · HSM-047 · 820m' },
    { time: '14:33:08', level: 'error', msg: 'Hedef kilitlendi · HSM-047' },
  ]);

  const startRef = useRef(Date.now());
  const trailRef = useRef([]);

  useEffect(() => {
    const id = setInterval(() => setTick(t => t + 1), 250);
    return () => clearInterval(id);
  }, []);

  const t = tick * 0.08;

  // Kendi İHA
  const ownAircraft = {
    lat: 41.02 + Math.sin(t * 0.08) * 0.015,
    lng: 29.01 + Math.cos(t * 0.08) * 0.018,
    alt: 185 + Math.sin(t * 0.3) * 8,
    speed: 24 + Math.sin(t * 0.4) * 2,
    heading: (t * 4) % 360,
    trail: trailRef.current,
  };

  // Trail biriktir
  useEffect(() => {
    if (tick % 6 === 0) {
      trailRef.current = [...trailRef.current.slice(-80), { lat: ownAircraft.lat, lng: ownAircraft.lng }];
    }
  }, [tick]);

  // Diğer takımların İHA'ları (TEKNOFEST API mock)
  const otherAircraft = [
    { id: 'HSM-047', team: 'hostile', lat: 41.028, lng: 29.022, alt: 190, speed: 22, heading: 220 + Math.sin(t * 0.2) * 15, range: 820, locked: lockedId === 'HSM-047' },
    { id: 'HSM-112', team: 'hostile', lat: 41.008, lng: 29.035, alt: 170, speed: 28, heading: 80 + Math.cos(t * 0.15) * 10, range: 1240, locked: lockedId === 'HSM-112' },
    { id: 'DST-003', team: 'friendly', lat: 41.015, lng: 28.988, alt: 200, speed: 26, heading: 145, range: 680, locked: false },
    { id: 'DST-019', team: 'friendly', lat: 41.032, lng: 29.005, alt: 175, speed: 23, heading: 305, range: 920, locked: false },
    { id: 'BLN-201', team: 'unknown', lat: 40.998, lng: 29.018, alt: 160, speed: 20, heading: 45, range: 1580, locked: false },
  ];

  // Waypoint'ler
  const [waypoints, setWaypoints] = useState([
    { type: 'takeoff', lat: 41.008, lng: 28.992, alt: 20 },
    { type: 'waypoint', lat: 41.015, lng: 29.005, alt: 150 },
    { type: 'waypoint', lat: 41.025, lng: 29.015, alt: 200 },
    { type: 'loiter', lat: 41.030, lng: 29.028, alt: 220 },
    { type: 'waypoint', lat: 41.018, lng: 29.035, alt: 180 },
    { type: 'waypoint', lat: 41.005, lng: 29.020, alt: 120 },
    { type: 'land', lat: 41.008, lng: 28.992, alt: 0 },
  ]);

  // Telemetri
  const telemetry = {
    battery: 23.8 - (t / 60) * 0.02,
    rssi: -58 + Math.sin(t * 0.5) * 4,
    hdop: 0.87 + Math.abs(Math.sin(t * 0.1)) * 0.2,
    throttle: 62 + Math.sin(t * 0.3) * 8,
    satellites: 14,
    temp: 42 + Math.sin(t * 0.2) * 3,
    current: 18.4 + Math.sin(t * 0.4) * 2,
    mah: Math.round(1200 + t * 4),
  };

  // Uçuş verileri (PFD)
  const flight = {
    pitch: Math.sin(t * 0.3) * 5,
    roll: Math.sin(t * 0.2) * 12,
    heading: ownAircraft.heading,
    airspeed: ownAircraft.speed,
    altitude: ownAircraft.alt,
    vspeed: Math.cos(t * 0.25) * 2,
  };

  // Bağlantı
  const connection = {
    quality: Math.round(88 + Math.sin(t * 0.5) * 6),
    utc: new Date(startRef.current + t * 1000).toISOString().slice(11, 19),
  };

  // Görev süresi
  const elapsed = Math.floor((Date.now() - startRef.current) / 1000);
  const missionTime = `${String(Math.floor(elapsed / 60)).padStart(2, '0')}:${String(elapsed % 60).padStart(2, '0')}`;

  // Kilitlenen hedef
  const lockedContact = otherAircraft.find(a => a.id === lockedId);
  const target = lockedContact ? {
    id: lockedContact.id,
    type: 'İHA · HASIM',
    range: lockedContact.range,
    confidence: 92,
  } : null;

  const addLog = useCallback((level, msg) => {
    const time = new Date().toISOString().slice(11, 19);
    setLogs(prev => [...prev.slice(-20), { time, level, msg }]);
  }, []);

  const handleAction = useCallback((action) => {
    if (action === 'ARM') { setArmed(true); addLog('success', 'İHA ARMED'); }
    else if (action === 'DISARM') { setArmed(false); addLog('warn', 'İHA DISARMED'); }
    else if (action === 'RTL') { setMode('RTL'); addLog('warn', 'Dönüş başlatıldı (Return To Launch)'); }
    else if (action === 'LAND') { setMode('LAND'); addLog('warn', 'İniş başlatıldı'); }
    else if (action === 'KILL') { setArmed(false); addLog('error', 'ACİL DURDUR tetiklendi!'); }
    else if (action === 'LOITER') { setMode('LOITER'); addLog('info', 'LOITER modunda'); }
    else if (action === 'AUTO') { setMode('AUTO'); addLog('info', 'AUTO moduna geçildi'); }
    else if (action === 'TAKEOFF') { addLog('info', 'Kalkış komutu gönderildi'); }
    else if (action === 'LOCK' || action === 'UNLOCK') { setLockedId(action === 'UNLOCK' ? null : 'HSM-047'); addLog(action === 'LOCK' ? 'error' : 'info', action === 'LOCK' ? 'Hedef kilitlendi' : 'Kilidi bırakıldı'); }
    else addLog('info', `Komut: ${action}`);
  }, [addLog]);

  // Uyarı sistemi
  const alertQ = window.useAlertQueue ? window.useAlertQueue() : { alerts: [], push: () => {} };

  // Otomatik uyarılar
  useEffect(() => {
    // Kilitlenme tamamlandı (kilitlendikten 2 sn sonra)
    if (lockedId && tick % 400 === 120) {
      alertQ.push('lock', `${lockedId} · kilitlenme tamamlandı (${target?.confidence || 92}%)`);
    }
    // HSS yaklaşma - sürekli rotasyon nedeniyle periyodik
    if (tick % 350 === 50) {
      alertQ.push('hss', 'HSS-01 menzili içine giriliyor · 320m');
    }
    // Sınır yaklaşma
    if (tick % 450 === 200) {
      alertQ.push('border', 'Uçuş sahası sınırına 180m · tampon bölge');
    }
    // Batarya kritik
    if (telemetry.battery < 21 && tick % 180 === 0) {
      alertQ.push('battery', `Batarya ${telemetry.battery.toFixed(1)}V · RTL önerilir`);
    }
    // Haberleşme gecikme
    if (connection.quality < 75 && tick % 220 === 0) {
      alertQ.push('comms', `Link kalitesi %${connection.quality} · gecikme yüksek`);
    }
  }, [tick]);

  return {
    tick, t,
    ownAircraft, otherAircraft, waypoints, setWaypoints,
    telemetry, flight, connection, missionTime,
    armed, mode, lockedId, setLockedId,
    target, logs, addLog,
    state: { armed, mode },
    handleAction,
    alerts: alertQ.alerts, pushAlert: alertQ.push,
  };
}

window.useSimulation = useSimulation;
