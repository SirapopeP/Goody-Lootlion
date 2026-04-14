/** อ่านค่า claim จาก JWT ฝั่ง client เพื่อแสดง UI เท่านั้น (ไม่ใช้ยืนยันตัวตน) */

export type HouseholdRoleClaim = 'parent' | 'child';

export interface JwtUserDisplay {
  userId: string | null;
  email: string | null;
  displayName: string | null;
  /** จาก claim `household_role` (เมื่อล็อกอินและ token มี claim) */
  householdRole: HouseholdRoleClaim | null;
}

function base64UrlToUtf8(segment: string): string {
  const pad = segment.length % 4 === 0 ? '' : '='.repeat(4 - (segment.length % 4));
  const b64 = segment.replace(/-/g, '+').replace(/_/g, '/') + pad;
  const binary = atob(b64);
  const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
  return new TextDecoder('utf-8').decode(bytes);
}

export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) {
      return null;
    }
    const json = base64UrlToUtf8(parts[1]);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function pickString(p: Record<string, unknown>, keys: string[]): string | null {
  for (const k of keys) {
    const v = p[k];
    if (typeof v === 'string' && v.length > 0) {
      return v;
    }
  }
  return null;
}

/** Map claim จากเซิร์ฟเวอร์ .NET / มาตรฐาน JWT */
function parseHouseholdRoleClaim(raw: string | null): HouseholdRoleClaim | null {
  if (!raw) {
    return null;
  }
  const n = raw.trim().toLowerCase();
  if (n === 'parent' || n === 'child') {
    return n;
  }
  return null;
}

export function parseJwtUserDisplay(token: string | null): JwtUserDisplay {
  const empty: JwtUserDisplay = {
    userId: null,
    email: null,
    displayName: null,
    householdRole: null,
  };
  if (!token) {
    return empty;
  }
  const p = decodeJwtPayload(token);
  if (!p) {
    return empty;
  }
  const userId = pickString(p, [
    'sub',
    'nameid',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
  ]);
  const email = pickString(p, [
    'email',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  ]);
  const displayName = pickString(p, [
    'display_name',
    'name',
    'unique_name',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
  ]);
  const householdRole = parseHouseholdRoleClaim(pickString(p, ['household_role']));
  return { userId, email, displayName, householdRole };
}
