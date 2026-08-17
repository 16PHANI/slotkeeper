import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { BookingItem } from "../types";

export function MyBookingsPage() {
  const { token } = useAuth();
  const [bookings, setBookings] = useState<BookingItem[]>([]);

  async function refresh() {
    const data = await api.get<BookingItem[]>("/api/bookings/mine", token);
    setBookings(data);
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function handleCancel(id: string) {
    await api.delete(`/api/bookings/${id}`, token);
    await refresh();
  }

  return (
    <div>
      <h1>My Bookings</h1>
      <table className="bookings-table">
        <thead>
          <tr>
            <th>Resource</th>
            <th>Start</th>
            <th>End</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {bookings.map((booking) => (
            <tr key={booking.id}>
              <td>{booking.resourceName ?? booking.resourceId}</td>
              <td>{new Date(booking.startUtc).toLocaleString()}</td>
              <td>{new Date(booking.endUtc).toLocaleString()}</td>
              <td>{booking.status}</td>
              <td>{booking.status === "Confirmed" && <button onClick={() => handleCancel(booking.id)}>Cancel</button>}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
