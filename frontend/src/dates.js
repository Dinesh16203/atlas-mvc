export const isoDate = d => {
  const date = new Date(d);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
};
export const todayIso = () => isoDate(new Date());
export const startOfWeek = input => {
  const d = new Date(`${input}T12:00:00`);
  const back = (d.getDay() + 6) % 7;
  d.setDate(d.getDate() - back);
  return isoDate(d);
};
export const addDays = (input, days) => {
  const d = new Date(`${input}T12:00:00`);
  d.setDate(d.getDate() + days);
  return isoDate(d);
};
export const weekDays = input => Array.from({ length: 7 }, (_, i) => addDays(input, i));
export const niceDate = (input, options = { month: 'short', day: 'numeric' }) =>
  new Date(`${input}T12:00:00`).toLocaleDateString('en-US', options);
export const fullWeek = start => `${niceDate(start)} – ${niceDate(addDays(start, 6), { month: 'short', day: 'numeric', year: 'numeric' })}`;
export const isoWeekInput = start => {
  const d = new Date(`${start}T12:00:00`), day = d.getDay() || 7;
  d.setDate(d.getDate() + 4 - day);
  const year = d.getFullYear(), jan1 = new Date(year, 0, 1);
  const week = Math.ceil((((d - jan1) / 86400000) + 1) / 7);
  return `${year}-W${String(week).padStart(2, '0')}`;
};
export const mondayFromIsoWeek = value => {
  const [year, week] = value.split('-W').map(Number);
  const jan4 = new Date(year, 0, 4, 12), day = jan4.getDay() || 7;
  jan4.setDate(jan4.getDate() - day + 1 + (week - 1) * 7);
  return isoDate(jan4);
};
export const money = v => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(v || 0);
export const hours = v => `${Number(v || 0).toFixed(1)}`;
