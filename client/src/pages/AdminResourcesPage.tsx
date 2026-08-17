import { FormEvent, useEffect, useState } from "react";
import { api } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { ResourceItem } from "../types";

export function AdminResourcesPage() {
  const { token } = useAuth();
  const [resources, setResources] = useState<ResourceItem[]>([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [location, setLocation] = useState("");
  const [slotMinutes, setSlotMinutes] = useState(30);
  const [maxPerDay, setMaxPerDay] = useState(2);

  async function refresh() {
    const data = await api.get<ResourceItem[]>("/api/resources", token);
    setResources(data);
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();

    await api.post(
      "/api/resources",
      {
        name,
        description,
        location,
        slotMinutes,
        maxBookingsPerUserPerDay: maxPerDay,
      },
      token
    );

    setName("");
    setDescription("");
    setLocation("");
    await refresh();
  }

  async function handleDeactivate(id: string) {
    await api.delete(`/api/resources/${id}`, token);
    await refresh();
  }

  return (
    <div>
      <h1>Manage Resources</h1>

      <form className="admin-form" onSubmit={handleCreate}>
        <h2>New resource</h2>
        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>
        <label>
          Description
          <input value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>
        <label>
          Location
          <input value={location} onChange={(e) => setLocation(e.target.value)} />
        </label>
        <label>
          Slot size (minutes)
          <input type="number" min={5} step={5} value={slotMinutes} onChange={(e) => setSlotMinutes(Number(e.target.value))} />
        </label>
        <label>
          Max bookings per user per day
          <input type="number" min={1} value={maxPerDay} onChange={(e) => setMaxPerDay(Number(e.target.value))} />
        </label>
        <button type="submit">Create resource</button>
      </form>

      <table className="bookings-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Location</th>
            <th>Slot size</th>
            <th>Daily limit</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {resources.map((resource) => (
            <tr key={resource.id}>
              <td>{resource.name}</td>
              <td>{resource.location}</td>
              <td>{resource.slotMinutes} min</td>
              <td>{resource.maxBookingsPerUserPerDay}</td>
              <td>
                <button onClick={() => handleDeactivate(resource.id)}>Deactivate</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
