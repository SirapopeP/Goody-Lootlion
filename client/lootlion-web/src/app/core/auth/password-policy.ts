/** สอดคล้องกับ ASP.NET Identity ใน API (DependencyInjection password options) */
export const PASSWORD_MIN_LENGTH = 8;

/** ตัวเล็ก + ใหญ่ + ตัวเลข + อักขระพิเศษ */
export const PASSWORD_PATTERN =
  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;
