export async function api(url, options = {}) {
  let response;
  try {
    response = await fetch(url, {
      credentials: 'include',
      ...options,
      headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
    });
  } catch {
    throw new Error('The ATLAS API is not available. Start the .NET server in the other terminal.');
  }
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.error || body.title || `Request failed (${response.status}).`);
  return body;
}

export const post = (url, body) => api(url, { method: 'POST', body: JSON.stringify(body) });
export const put = (url, body) => api(url, { method: 'PUT', body: JSON.stringify(body) });
export const del = url => api(url, { method: 'DELETE' });
