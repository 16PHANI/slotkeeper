export type UserRole = "Member" | "Admin";

export interface AuthResponse {
  token: string;
  displayName: string;
  role: UserRole;
  expiresUtc: string;
}

export interface ResourceItem {
  id: string;
  name: string;
  description: string;
  location: string;
  slotMinutes: number;
  maxBookingsPerUserPerDay: number;
  isActive: boolean;
}

export interface BookingItem {
  id: string;
  resourceId: string;
  resourceName: string | null;
  startUtc: string;
  endUtc: string;
  status: string;
  createdUtc: string;
}
