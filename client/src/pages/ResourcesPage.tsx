import { FormEvent, useEffect, useState } from "react";
import { api, ApiError } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { ResourceItem } from "../types";

export function ResourcesPage() {
  const { token } = useAuth();
  const [resources, setResources] = useState<ResourceItem[]>([]);
  const [selectedId, setSelectedId] = useState("");
  const [date, setDate] = useState("");
  const [time, setTime] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<ResourceItem[]>("/api/resources", token).then(setResources).catch(() => setResources([]));
  }, [token]);

  const selectedResource = resources.find((r) => r.id === selectedId);

  async function handleBook(event: FormEvent) {
    event.preventDefault();
    setMessage(null);
    setError(null);

    if (!selectedResource || !date || !time) {
      setError("Pick a resource, date, and time first.");
      return;
    }

    const startUtc = new Date(`${date}T${time}:00`);
    const endUtc = new Date(startUtc.getTime() + selectedResource.slotMinutes * 60_000);

    try {
      await api.post(
        "/api/bookings",
        {
          resourceId: selectedResource.id,
          startUtc: startUtc.toISOString(),
          endUtc: endUtc.toISOString(),
        },
        token
      );

      setMessage("Booked. Check My Bookings to see it.");
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        const joinWaitlist = window.confirm(`${err.message}\n\nJoin the waitlist for this slot instead?`);

        if (joinWaitlist) {
          await api.post(
            "/api/bookings/waitlist",
            {
              resourceId: selectedResource.id,
              desiredStartUtc: startUtc.toISOString(),
              desiredEndUtc: endUtc.toISOString(),
            },
            token
          );
          setMessage("Added to the waitlist. You'll be booked automatically if the slot frees up.");
          return;
        }
      }

      setError(err instanceof ApiError ? err.message : "Booking failed.");
    }
  }

  return (
    <div>
      <h1>Resources</h1>
      <div className="resource-grid">
        {resources.map((resource) => (
          <div
            key={resource.id}
            className={`resource-card ${resource.id === selectedId ? "selected" : ""}`}
            onClick={() => setSelectedId(resource.id)}
          >
            <h3>{resource.name}</h3>
            <p>{resource.description}</p>
            <p className="muted">{resource.location}</p>
            <p className="muted">
              {resource.slotMinutes}-minute slots, {resource.maxBookingsPerUserPerDay}/day limit
            </p>
          </div>
        ))}
      </div>

      <form className="booking-form" onSubmit={handleBook}>
        <h2>Book {selectedResource ? selectedResource.name : "a resource"}</h2>
        <label>
          Date
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
        </label>
        <label>
          Start time
          <input type="time" value={time} onChange={(e) => setTime(e.target.value)} required />
        </label>
        {message && <p className="form-success">{message}</p>}
        {error && <p className="form-error">{error}</p>}
        <button type="submit" disabled={!selectedResource}>
          Book slot
        </button>
      </form>
    </div>
  );
}
