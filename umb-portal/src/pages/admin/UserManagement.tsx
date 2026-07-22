import React, { useEffect, useState } from "react";
import adminService from "../../services/adminService";
import type { AdminUserDTO } from "../../services/adminService";
import { Users, Search, Plus } from "lucide-react";
import { useToast } from "../../Components/Toast";
import { getInitials } from "../../utils/formatters";

const UserRow: React.FC<{
  user: AdminUserDTO;
  onToggle: (id: number) => void;
  toggling: boolean;
}> = ({ user, onToggle, toggling }) => (
  <tr className="border-b border-gray-50 hover:bg-gray-50">
    <td className="py-3 px-4">
      <div className="flex items-center gap-3">
        <div
          className={`h-8 w-8 flex items-center justify-center rounded-full ${user.isAdmin ? "bg-amber-100 text-amber-800" : "bg-blue-100 text-blue-800"} text-sm`}
        >
          {getInitials(user.fullName)}
        </div>
        <div>
          <div className="font-medium">{user.fullName}</div>
          <div className="text-sm text-gray-500">{user.username}</div>
        </div>
      </div>
    </td>
    <td className="py-3 px-4 text-sm text-gray-500">{user.email}</td>
    <td className="py-3 px-4">
      <span
        className={`rounded-full px-2 py-0.5 text-xs ${user.isAdmin ? "bg-amber-100 text-amber-800" : "bg-blue-100 text-blue-800"}`}
      >
        {user.isAdmin ? "Admin" : "Staff"}
      </span>
    </td>
    <td className="py-3 px-4">
      <span
        className={`rounded-full px-2 py-0.5 text-xs ${user.isActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800"}`}
      >
        {user.isActive ? "Active" : "Disabled"}
      </span>
    </td>
    <td className="py-3 px-4">
      <button
        disabled={toggling}
        onClick={() => onToggle(user.id)}
        className={`rounded-lg px-3 py-1.5 text-sm ${user.isActive ? "border border-gray-200 text-red-600 hover:text-red-700" : "border border-gray-200 text-green-600 hover:text-green-700"}`}
      >
        {toggling ? "..." : user.isActive ? "Disable" : "Enable"}
      </button>
    </td>
  </tr>
);

const AddUserModal: React.FC<{
  onClose: () => void;
  onAdded: (u: AdminUserDTO) => void;
}> = ({ onClose, onAdded }) => {
  const [username, setUsername] = useState("");
  const [lookupStatus, setLookupStatus] = useState<
    "idle" | "loading" | "found" | "notfound" | "error"
  >("idle");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [isAdmin, setIsAdmin] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const { showToast } = useToast();

  useEffect(() => {
    if (!username || username.trim().length < 3) return;

    const t = setTimeout(() => {
      setLookupStatus("loading");
      adminService
        .adLookup(username.trim())
        .then((r) => {
          if (r.found) {
            setLookupStatus("found");
            setFullName(r.fullName);
            setEmail(r.email);
          } else {
            setLookupStatus("notfound");
            setFullName("");
            setEmail("");
          }
        })
        .catch(() => setLookupStatus("error"));
    }, 500);

    return () => clearTimeout(t);
  }, [username]);

  const submit = async () => {
    if (lookupStatus !== "found") return;
    setSubmitting(true);
    try {
      const created = await adminService.addUser(username.trim(), isAdmin);
      onAdded(created);
      showToast("User added successfully", "success");
      onClose();
    } catch {
      showToast("Unable to add user", "error");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative z-10 w-full max-w-md bg-white rounded-xl border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="text-lg font-medium">Add New User</div>
          <button onClick={onClose} className="text-gray-500">
            ×
          </button>
        </div>
        <div className="space-y-3">
          <div>
            <label className="block text-sm text-gray-600">AD Username</label>
            <input
              value={username}
              onChange={(e) => {
                const v = e.target.value;
                setUsername(v);
                if (!v || v.trim().length < 3) {
                  setLookupStatus("idle");
                  setFullName("");
                  setEmail("");
                }
              }}
              className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2"
              placeholder="e.g. john.doe"
            />
            <div className="mt-1 text-sm text-gray-500">
              {lookupStatus === "loading" && <span>Looking up user...</span>}
              {lookupStatus === "found" && (
                <span className="text-green-700">Found: {fullName}</span>
              )}
              {lookupStatus === "notfound" && (
                <span className="text-red-600">User not found in AD</span>
              )}
              {lookupStatus === "error" && (
                <span className="text-orange-600">Unable to reach AD</span>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm text-gray-600">Full Name</label>
            <input
              value={fullName}
              readOnly
              className="mt-1 w-full border border-gray-200 rounded-lg px-3 py-2 bg-gray-50"
              placeholder="Auto-filled from Active Directory"
            />
          </div>

          <div>
            <label className="block text-sm text-gray-600">Email</label>
            <input
              value={email}
              readOnly
              className="mt-1 w-full border border-gray-200 rounded-lg px-3 py-2 bg-gray-50"
              placeholder="Auto-filled from Active Directory"
            />
          </div>

          <div>
            <label htmlFor="roleSelect" className="block text-sm text-gray-600">
              Role
            </label>
            <select
              id="roleSelect"
              value={isAdmin ? "admin" : "staff"}
              onChange={(e) => setIsAdmin(e.target.value === "admin")}
              className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2"
            >
              <option value="staff">Staff</option>
              <option value="admin">Admin</option>
            </select>
          </div>

          <div className="flex items-center justify-end gap-2 mt-4">
            <button
              onClick={onClose}
              className="rounded-lg border border-gray-300 px-4 py-2 text-sm"
            >
              Cancel
            </button>
            <button
              disabled={lookupStatus !== "found" || submitting}
              onClick={submit}
              className="rounded-lg bg-[#E6A817] px-4 py-2 text-sm font-medium text-[#1a1000]"
            >
              {submitting ? "Adding..." : "Add User"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<AdminUserDTO[]>([]);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [loading, setLoading] = useState(true);
  const [togglingId, setTogglingId] = useState<number | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const { showToast } = useToast();

  useEffect(() => {
    const debounced = setTimeout(async () => {
      setLoading(true);
      try {
        const data = await adminService.getUsers(search, status);
        setUsers(data);
      } catch {
        showToast("Unable to load users", "error");
      } finally {
        setLoading(false);
      }
    }, 300);
    return () => clearTimeout(debounced);
  }, [search, status, showToast]);

  const handleToggle = async (id: number) => {
    setTogglingId(id);
    try {
      const updated = await adminService.toggleUser(id);
      setUsers((s) => s.map((u) => (u.id === updated.id ? updated : u)));
      showToast("User updated", "success");
    } catch {
      showToast("Unable to update user", "error");
    } finally {
      setTogglingId(null);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Users />
          <div className="text-lg font-medium">Authorized Users</div>
        </div>
        <div>
          <button
            onClick={() => setModalOpen(true)}
            className="flex items-center gap-2 rounded-lg bg-[#E6A817] px-3 py-2 text-sm text-[#1a1000]"
          >
            <Plus size={14} /> Add User
          </button>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 p-4">
        <div className="flex items-center gap-3 mb-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-3 text-gray-400" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by name or username..."
              className="w-full rounded-lg border border-gray-300 px-10 py-2 text-sm"
            />
          </div>

          <select
            aria-label="Filter status"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="">All Status</option>
            <option value="active">Active</option>
            <option value="disabled">Disabled</option>
          </select>
        </div>

        {loading ? (
          <div className="animate-pulse">Loading users...</div>
        ) : users.length === 0 ? (
          <div className="py-10 text-center text-gray-500">No users found</div>
        ) : (
          <table className="w-full text-left">
            <thead className="text-xs text-gray-400 uppercase tracking-wider border-b border-gray-100">
              <tr>
                <th className="py-3 px-4">User</th>
                <th className="py-3 px-4">Email</th>
                <th className="py-3 px-4">Role</th>
                <th className="py-3 px-4">Status</th>
                <th className="py-3 px-4">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <UserRow
                  key={u.id}
                  user={u}
                  onToggle={handleToggle}
                  toggling={togglingId === u.id}
                />
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modalOpen && (
        <AddUserModal
          onClose={() => setModalOpen(false)}
          onAdded={(user) => setUsers((s) => [user, ...s])}
        />
      )}
    </div>
  );
};

export default UserManagement;
