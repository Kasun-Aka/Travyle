import { useState, useEffect } from "react";
import { auth } from "../lib/firebase";

export type AgentWorkflow = {
  id: string;
  travelerId: string;
  travelerName: string;
  travelerEmail: string;
  objective: string;
  status: string;
  plan: string[];
  completedSteps: string[];
  toolResults: Record<string, any>;
  validationResults: Record<string, boolean>;
  proposedBooking?: {
    scheduleId: string;
    destinationTitle: string;
    location: string;
    bookingDate: string;
    timeSlot: string;
    guests: number;
    pricePerPerson: number;
    basePrice: number;
    serviceFee: number;
    discountAmount: number;
    totalAmount: number;
    paymentMethod: string;
  };
  createdBookingId?: string;
  bookingReference?: string;
  approvalStatus: string;
  approvedBy?: string;
  approverRole?: string;
  approverNotes?: string;
  errorMessage?: string;
  createdAt: string;
};

type Props = {
  apiUrl: string;
  onBookingApproved?: () => void;
};

export default function AgentApprovals({ apiUrl, onBookingApproved }: Props) {
  const [workflows, setWorkflows] = useState<AgentWorkflow[]>([]);
  const [approvedWorkflows, setApprovedWorkflows] = useState<AgentWorkflow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionNotice, setActionNotice] = useState<string | null>(null);
  const [processingId, setProcessingId] = useState<string | null>(null);
  const [rejectReasonModal, setRejectReasonModal] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState("");

  const authHeaders = async (): Promise<Record<string, string>> => {
    const token = await auth?.currentUser?.getIdToken();
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };
    if (token) headers.Authorization = `Bearer ${token}`;
    return headers;
  };

  const getErrorMessage = async (response: Response) => {
    const body = await response.json().catch(() => null);
    if (typeof body === "string") return body;
    if (body && typeof body === "object") {
      const error = body as {
        error?: string;
        detail?: string;
        title?: string;
        errors?: Record<string, string[]>;
      };
      const validationErrors = Object.values(error.errors ?? {}).flat();
      return (
        error.error ||
        error.detail ||
        validationErrors.join(" ") ||
        error.title ||
        `Request failed (${response.status}).`
      );
    }
    return `Request failed (${response.status}).`;
  };

  const loadWorkflows = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(`${apiUrl}/agent/workflows/pending`);
      if (res.ok) {
        const data = await res.json();
        setWorkflows(data);
      } else {
        setError(`Failed to fetch pending workflows (${res.status})`);
      }
    } catch (e: any) {
      setError(e.message || "Failed to connect to backend agent API");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadWorkflows();
    const interval = setInterval(loadWorkflows, 10000);
    return () => clearInterval(interval);
  }, [apiUrl]);

  const approvedWorkflowIds = new Set(approvedWorkflows.map((wf) => wf.id));
  const displayedWorkflows = [
    ...approvedWorkflows,
    ...workflows.filter((wf) => !approvedWorkflowIds.has(wf.id)),
  ];

  const handleApprove = async (workflowId: string) => {
    setProcessingId(workflowId);
    setActionNotice(null);
    try {
      const res = await fetch(`${apiUrl}/agent/workflows/${workflowId}/approve`, {
        method: "POST",
        headers: await authHeaders(),
        body: JSON.stringify({
          approverRole: "Admin",
          approverNotes: "Approved by Operations Admin",
        }),
      });

      if (res.ok) {
        const data = await res.json();
        const workflow = workflows.find((wf) => wf.id === workflowId);
        if (workflow) {
          setApprovedWorkflows((current) => [
            ...current.filter((wf) => wf.id !== workflowId),
            {
              ...workflow,
              status: data.status || "Completed",
              approvalStatus: data.approvalStatus || "APPROVED",
              approvedBy: data.approvedBy || "Admin",
              bookingReference: data.bookingReference || workflow.bookingReference,
              createdBookingId: data.createdBookingId || workflow.createdBookingId,
            },
          ]);
        }
        setActionNotice(`Workflow approved! Created booking: ${data.bookingReference || "BKG-CONFIRMED"}`);
        loadWorkflows();
        if (onBookingApproved) onBookingApproved();
      } else {
        alert(`Approval failed: ${await getErrorMessage(res)}`);
      }
    } catch (e: any) {
      alert(`Network error: ${e.message}`);
    } finally {
      setProcessingId(null);
    }
  };

  const handleReject = async () => {
    if (!rejectReasonModal) return;
    const workflowId = rejectReasonModal;
    setProcessingId(workflowId);
    try {
      const res = await fetch(`${apiUrl}/agent/workflows/${workflowId}/reject`, {
        method: "POST",
        headers: await authHeaders(),
        body: JSON.stringify({
          approverRole: "Admin",
          reason: rejectReason || "Rejected by Operations Admin",
        }),
      });

      if (res.ok) {
        setActionNotice(`Workflow rejected successfully.`);
        setRejectReasonModal(null);
        setRejectReason("");
        loadWorkflows();
      } else {
        alert(`Reject failed: ${await getErrorMessage(res)}`);
      }
    } catch (e: any) {
      alert(`Network error: ${e.message}`);
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <div style={{ padding: "24px", maxWidth: "1100px", margin: "0 auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px" }}>
        <div>
          <h2 style={{ margin: "0 0 4px 0", fontSize: "22px", color: "#111827" }}>
            Smart Booking Agent — Operator Approvals
          </h2>
          <p style={{ margin: 0, color: "#6b7280", fontSize: "14px" }}>
            Review AI-proposed bookings. High-impact bookings require human authorization before execution.
          </p>
        </div>
        <button
          onClick={loadWorkflows}
          disabled={loading}
          style={{
            padding: "8px 16px",
            background: "#10b981",
            color: "#fff",
            border: "none",
            borderRadius: "8px",
            cursor: "pointer",
            fontWeight: 600,
          }}
        >
          {loading ? "Refreshing..." : "Refresh"}
        </button>
      </div>

      {actionNotice && (
        <div style={{ background: "#ecfdf5", border: "1px solid #a7f3d0", color: "#065f46", padding: "12px 16px", borderRadius: "8px", marginBottom: "16px" }}>
          {actionNotice}
        </div>
      )}

      {error && (
        <div style={{ background: "#fef2f2", border: "1px solid #fecaca", color: "#991b1b", padding: "12px 16px", borderRadius: "8px", marginBottom: "16px" }}>
          {error}
        </div>
      )}

      {displayedWorkflows.length === 0 && !loading && (
        <div style={{ textAlign: "center", padding: "60px 20px", background: "#f9fafb", borderRadius: "12px", border: "1px dashed #d1d5db" }}>
          <div style={{ fontSize: "36px", marginBottom: "12px" }}>🤖✨</div>
          <h3 style={{ margin: "0 0 6px 0", color: "#374151" }}>No Pending Agent Proposals</h3>
          <p style={{ margin: 0, color: "#9ca3af", fontSize: "14px" }}>
            When travelers initiate smart bookings from the mobile app, proposals awaiting approval appear here.
          </p>
        </div>
      )}

      <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
        {displayedWorkflows.map((wf) => {
          const prop = wf.proposedBooking;
          const isBusy = processingId === wf.id;
          const isApproved = wf.approvalStatus.toUpperCase() === "APPROVED";

          return (
            <div
              key={wf.id}
              style={{
                background: "#ffffff",
                border: "1px solid #e5e7eb",
                borderRadius: "12px",
                padding: "20px",
                boxShadow: "0 1px 3px rgba(0,0,0,0.05)",
              }}
            >
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "12px" }}>
                <div>
                  <span
                    style={{
                      background: isApproved ? "#dcfce7" : "#fef3c7",
                      color: isApproved ? "#166534" : "#92400e",
                      fontSize: "12px",
                      fontWeight: 700,
                      padding: "4px 8px",
                      borderRadius: "6px",
                      textTransform: "uppercase",
                      letterSpacing: "0.5px",
                    }}
                  >
                    {isApproved ? "Approved" : "Awaiting Admin Approval"}
                  </span>
                  <h3 style={{ margin: "8px 0 2px 0", fontSize: "18px", color: "#111827" }}>
                    {prop ? prop.destinationTitle : "Tour Booking"}
                  </h3>
                  <div style={{ color: "#4b5563", fontSize: "13px" }}>
                    Traveler: <b>{wf.travelerName || "Guest"}</b> ({wf.travelerEmail}) &bull; Objective: &ldquo;{wf.objective}&rdquo;
                  </div>
                </div>

                <div style={{ textAlign: "right" }}>
                  <div style={{ fontSize: "20px", fontWeight: 800, color: "#059669" }}>
                    LKR {prop ? prop.totalAmount.toLocaleString() : "0"}
                  </div>
                  <div style={{ fontSize: "12px", color: "#6b7280" }}>
                    {prop ? `${prop.guests} Guests &bull; ${prop.timeSlot}` : ""}
                  </div>
                </div>
              </div>

              {prop && (
                <div
                  style={{
                    background: "#f8fafc",
                    padding: "12px 16px",
                    borderRadius: "8px",
                    fontSize: "13px",
                    color: "#334155",
                    marginBottom: "16px",
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))",
                    gap: "8px",
                  }}
                >
                  <div><b>Date:</b> {new Date(prop.bookingDate).toLocaleDateString()}</div>
                  <div><b>Slot:</b> {prop.timeSlot}</div>
                  <div><b>Rate/person:</b> LKR {prop.pricePerPerson.toLocaleString()}</div>
                  <div><b>Service fee:</b> LKR {prop.serviceFee.toLocaleString()}</div>
                  <div><b>Discount:</b> LKR {prop.discountAmount.toLocaleString()}</div>
                  <div><b>Payment Method:</b> {prop.paymentMethod}</div>
                </div>
              )}

              {/* Validation checks summary */}
              <div style={{ marginBottom: "16px", fontSize: "12px", color: "#64748b" }}>
                <b>Deterministic Rules Checked:</b>{" "}
                {Object.entries(wf.validationResults || {}).map(([key, ok]) => (
                  <span
                    key={key}
                    style={{
                      display: "inline-block",
                      marginRight: "8px",
                      color: ok ? "#16a34a" : "#dc2626",
                    }}
                  >
                    {ok ? "✓" : "✗"} {key.replace(/_/g, " ")}
                  </span>
                ))}
              </div>

              <div style={{ display: "flex", gap: "12px", justifyContent: "flex-end" }}>
                <button
                  disabled={isBusy || isApproved}
                  onClick={() => {
                    setRejectReasonModal(wf.id);
                    setRejectReason("");
                  }}
                  style={{
                    padding: "8px 18px",
                    background: "#fef2f2",
                    color: "#b91c1c",
                    border: "1px solid #fecaca",
                    borderRadius: "6px",
                    cursor: "pointer",
                    fontWeight: 600,
                  }}
                >
                  Reject Proposal
                </button>
                <button
                  disabled={isBusy || isApproved}
                  onClick={() => handleApprove(wf.id)}
                  style={{
                    padding: "8px 20px",
                    background: isApproved ? "#ecfdf5" : "#10b981",
                    color: isApproved ? "#047857" : "#ffffff",
                    border: isApproved ? "1px solid #a7f3d0" : "none",
                    borderRadius: "6px",
                    cursor: "pointer",
                    fontWeight: 600,
                  }}
                >
                  {isApproved ? "Approved" : isBusy ? "Executing..." : "Approve & Execute Booking"}
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* Reject Modal */}
      {rejectReasonModal && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: "rgba(0,0,0,0.5)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 9999,
          }}
        >
          <div style={{ background: "#fff", padding: "24px", borderRadius: "12px", width: "420px" }}>
            <h3 style={{ margin: "0 0 12px 0" }}>Reject Agent Proposal</h3>
            <p style={{ fontSize: "14px", color: "#6b7280", margin: "0 0 12px 0" }}>
              Please provide a reason for rejecting this proposal:
            </p>
            <textarea
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              placeholder="e.g. Schedule capacity reserved for maintenance"
              rows={3}
              style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #d1d5db", marginBottom: "16px", boxSizing: "border-box" }}
            />
            <div style={{ display: "flex", gap: "10px", justifyContent: "flex-end" }}>
              <button
                onClick={() => setRejectReasonModal(null)}
                style={{ padding: "8px 16px", background: "#f3f4f6", border: "1px solid #d1d5db", borderRadius: "6px", cursor: "pointer" }}
              >
                Cancel
              </button>
              <button
                onClick={handleReject}
                style={{ padding: "8px 16px", background: "#ef4444", color: "#fff", border: "none", borderRadius: "6px", cursor: "pointer", fontWeight: 600 }}
              >
                Confirm Rejection
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
