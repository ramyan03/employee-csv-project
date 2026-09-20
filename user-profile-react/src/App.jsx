import { useEffect, useMemo, useState } from "react";

const STORAGE_KEY = "user_profile_v1";

/*
========================================
User Profile React App
========================================

What this app does:
- Displays a user profile
- Supports edit mode with Save / Undo / Cancel
- Persists saved data in localStorage so it survives reloads
- Profile is the commited/saved state while draft is the working copy
- Uses favoriteColor to style buttons
- Shows "Happy Birthday" when today matches birthdate
- Displays a stable stock photo header (picsum seed based on name)
- Allows user to edit freely, undo changes, or cancel wihtout corrupting saved state -> Similar to how Git works

Key design decision (state management):
- profile = committed/persisted state (source of truth)
- draft   = editable working copy (allows undo/cancel without mutating saved state)
This mirrors real apps where edits are staged before commit.
Single state object would require complex history tracking or cloning to implement undo
*/

const defaultProfile = {
  firstName: "Jane",
  lastName: "Doe",
  email: "jane.doe@example.com",
  phoneNumber: "+1 416 555 0123",
  address: "123 King St W, Toronto, ON",
  country: "Canada",
  hobbies: "Gym, anime, cooking",
  favoriteColor: "#3b82f6",
  birthdate: "2000-01-21", 
};

// Safely parses JSON from localStorage and validates that all required profile keys exist. 
function safeParseProfile(raw) {
  try {
    const obj = JSON.parse(raw);
    if (!obj || typeof obj !== "object") return null;
    for (const k of Object.keys(defaultProfile)) {
      if (!(k in obj)) return null;
    }
    return obj;
  } catch {
    return null;
  }
}

// Validate each key to prevent crashes if localStorage has old/corrupted data
function parseLocalYyyyMmDd(dateStr) {
  if (!dateStr) return null;
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateStr);
  if (!m) return null;
  const y = Number(m[1]);
  const mo = Number(m[2]);
  const d = Number(m[3]);
  const dt = new Date(y, mo - 1, d); 
  if (Number.isNaN(dt.getTime())) return null;
  return dt;
}

// Checks if the given birthdate string matches today's month and day (ignoring year) to determine if it's the user's birthday.
function isBirthdayToday(birthdateStr) {
  const bd = parseLocalYyyyMmDd(birthdateStr);
  if (!bd) return false;

  const now = new Date();
  return bd.getMonth() === now.getMonth() && bd.getDate() === now.getDate();
}

// Calculates the optimal text color (black or white) for a given background hex color by computing its luminance.
function bestTextColor(bg) {
  if (!bg || typeof bg !== "string") return "#ffffff";
  const hex = bg.startsWith("#") ? bg.slice(1) : bg;
  if (hex.length !== 6) return "#ffffff";
  const r = parseInt(hex.slice(0, 2), 16);
  const g = parseInt(hex.slice(2, 4), 16);
  const b = parseInt(hex.slice(4, 6), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255; 
  return luminance > 0.6 ? "#111827" : "#ffffff";
}

function Field({ label, value }) {
  return (
    <div className="fieldCard">
      <div className="label">{label}</div>
      <div className="value">{value || <span className="muted">—</span>}</div>
    </div>
  );
}

function InputField({ label, id, type = "text", placeholder = "", value, onChange }) {
  return (
    <div className="fieldCard">
      <label className="label" htmlFor={id}> 
        {label}
      </label>
      <input
        id={id}
        className="input"
        type={type}
        value={value}
        placeholder={placeholder}
        onChange={onChange}
      />
    </div>
  );
}

// Main App component
export default function App() {
  // Manages profile (saved state), draft (working copy), isEditing flag, and statusMsg.
  const [profile, setProfile] = useState(defaultProfile);
  const [draft, setDraft] = useState(defaultProfile);
  const [isEditing, setIsEditing] = useState(false);
  const [statusMsg, setStatusMsg] = useState("");

  useEffect(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return;
    const parsed = safeParseProfile(raw);
    if (parsed) {
      setProfile(parsed);
      setDraft(parsed);
    }
  }, []);

  useEffect(() => {
    if(statusMsg){
      const timer = setTimeout(() => {
        setStatusMsg("");
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [statusMsg])

  // Memoizes the birthday check calculation so it only recomputes when the birthdate changes, avoiding unnecessary recalculations on every render.
  const happyBirthday = useMemo(
    () => isBirthdayToday(profile.birthdate),
    [profile.birthdate]
  );

  const buttonBg =
    (isEditing ? draft.favoriteColor : profile.favoriteColor) || "#3b82f6";
  const buttonText = bestTextColor(buttonBg);


  const photoSeed = `${profile.firstName}-${profile.lastName}`.toLowerCase();
  const stockPhotoUrl = `https://picsum.photos/seed/${encodeURIComponent(
    photoSeed
  )}/1100/320`;


  function enterEditMode() {
    setDraft({ ...profile });
    setIsEditing(true);
    setStatusMsg("");
  }

  // Discards draft changes by resetting it to the saved profile and exits edit mode. 
  // Shows a "canceled" message.
  function cancelEdit() {
    setDraft({ ...profile }); // Revert to saved
    setIsEditing(false);
    setStatusMsg("Edits canceled.");
  }

  function undoChanges() {
    setDraft({ ...profile }); 
    setStatusMsg("Changes undone.");
  }

  // Save is the "commit": validate draft, persist to localStorage, then promote draft -> profile.
  function saveChanges() {
    if (!draft.email.trim()) {
      setStatusMsg("Email cannot be empty.");
      return;
    }
    if(!/^[a-zA-Z0-9]+a-zA-Z0-@[9]+\\.[A-Za-z]{2,3}$/.test(draft.email)){
      setStatusMsg("Email is not formatted correctly.");
      return;
    }
    if (!draft.firstName.trim() || !draft.lastName.trim()) {
      setStatusMsg("First and last name cannot be empty.");
      return;
    }
    if (draft.favoriteColor && !/^#([0-9a-fA-F]{6})$/.test(draft.favoriteColor)) {
      setStatusMsg("Favorite color must be a hex value like #22c55e.");
      return;
    }
    // Ensure date is valid AND formatted as YYYY-MM-DD
    if (draft.birthdate && !parseLocalYyyyMmDd(draft.birthdate)) {
      setStatusMsg("Birthdate must be a valid date.");
      return;
    }

    setProfile(draft); // Commit
    localStorage.setItem(STORAGE_KEY, JSON.stringify(draft));
    setIsEditing(false);
    setStatusMsg("Saved successfully.");

  }

  function handleDraftChange(key, value) {
    setDraft((prev) => ({ ...prev, [key]: value }));
    setStatusMsg("");
  }


  return (
    <div className="page">
      <div className="card">
        {/* Stock photo header */}{// Cosmetic enhancement: deterministic "random" header image so it stays stable per user.
}
        <div className="hero">
          <img className="heroImg" src={stockPhotoUrl} alt="Profile header" />
          <div className="heroOverlay" />
          <div className="heroText">
            <div className="heroTitle">
              {profile.firstName} {profile.lastName}
            </div>
            <div className="heroSub">User profile • Editable • Saved locally</div>
          </div>
        </div>

        <div className="header">
          <div>
            <h1 className="title">Profile Details</h1>
            <p className="subtitle">
              Edit your details, save your information, or undo/cancel changes.
            </p>
          </div>

          {!isEditing ? (
            <button
              className="btn"
              style={{ backgroundColor: buttonBg, color: buttonText }}
              onClick={enterEditMode}
            >
              Edit
            </button>
          ) : (
            <div className="btnRow">
              <button
                className="btn"
                style={{ backgroundColor: buttonBg, color: buttonText }}
                onClick={saveChanges}
              >
                Save
              </button>
              <button
                className="btn outline"
                style={{ borderColor: buttonBg, color: buttonBg }}
                onClick={undoChanges}
              >
                Undo
              </button>
              <button className="btn ghost" onClick={cancelEdit}>
                Cancel
              </button>
            </div>
          )}
        </div>

        {happyBirthday && (
          <div className="banner">🎉 Happy Birthday, {profile.firstName}!</div>
        )}

        {statusMsg && <div className="status">{statusMsg}</div>}

        {/* Centered grid container */}
        <div className="gridWrap">
          {!isEditing ? (
            <div className="grid">
              <Field label="First Name" value={profile.firstName} />
              <Field label="Last Name" value={profile.lastName} />
              <Field label="Email Address" value={profile.email} />
              <Field label="Phone Number" value={profile.phoneNumber} />
              <Field label="Address" value={profile.address} />
              <Field label="Country" value={profile.country} />
              <Field label="Hobbies / Interest" value={profile.hobbies} />
              <Field label="Favorite Color" value={profile.favoriteColor} />
              <Field label="Birthdate" value={profile.birthdate} />
            </div>
          ) : (
            <div className="grid">
              <InputField
                label="First Name"
                id="firstName"
                value={draft.firstName ?? ""}
                onChange={(e) => handleDraftChange("firstName", e.target.value)}
              />
              <InputField
                label="Last Name"
                id="lastName"
                value={draft.lastName ?? ""}
                onChange={(e) => handleDraftChange("lastName", e.target.value)}
              />
              <InputField
                label="Email Address"
                id="email"
                type="email"
                value={draft.email ?? ""}
                onChange={(e) => handleDraftChange("email", e.target.value)}
              />
              <InputField
                label="Phone Number"
                id="phoneNumber"
                placeholder="+1 416 555 0123"
                value={draft.phoneNumber ?? ""}
                onChange={(e) => handleDraftChange("phoneNumber", e.target.value)}
              />
              <InputField
                label="Address"
                id="address"
                value={draft.address ?? ""}
                onChange={(e) => handleDraftChange("address", e.target.value)}
              />
              <InputField
                label="Country"
                id="country"
                value={draft.country ?? ""}
                onChange={(e) => handleDraftChange("country", e.target.value)}
              />
              <InputField
                label="Hobbies / Interest"
                id="hobbies"
                value={draft.hobbies ?? ""}
                onChange={(e) => handleDraftChange("hobbies", e.target.value)}
              />
              <InputField
                label="Favorite Color (hex)"
                id="favoriteColor"
                placeholder="color"
                type="color"
                value={draft.favoriteColor ?? ""}
                onChange={(e) => handleDraftChange("favoriteColor", e.target.value)}
              />
              <InputField
                label="Birthdate"
                id="birthdate"
                type="date"
                value={draft.birthdate ?? ""} // must be YYYY-MM-DD
                onChange={(e) => handleDraftChange("birthdate", e.target.value)}
              />
            </div>
          )}
        </div>

        <div className="footer">
          <small className="muted">Saved in localStorage • key: {STORAGE_KEY}</small>
        </div>
      </div>

      <style>{`
        html, body, #root {
          height: 100%;
          width: 100%;
          margin: 0;           
          padding: 0;
        }
        :root {
          font-family: system-ui, -apple-system, Segoe UI, Roboto, Helvetica, Arial, "Apple Color Emoji","Segoe UI Emoji";
          color: #111827;
          background:
            radial-gradient(900px 600px at 10% 10%, rgba(59,130,246,0.15), transparent 60%),
            radial-gradient(800px 500px at 90% 20%, rgba(16,185,129,0.12), transparent 60%),
            linear-gradient(180deg, #f8fafc 0%, #eef2f7 100%);
        }

        .page {
          min-height: 100vh;
          width: 100%;  
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 24px;
        }
        .card {
          width: 100%;
          max-width: 980px;
          background: white;
          border-radius: 18px;
          box-shadow: 0 16px 40px rgba(0,0,0,0.10);
          overflow: hidden;
          margin: 0 auto; 
        }

        /* Hero (stock photo) */
        .hero { position: relative; height: 220px; }
        .heroImg { width: 100%; height: 100%; object-fit: cover; display: block; }
        .heroOverlay {
          position: absolute; inset: 0;
          background: linear-gradient(0deg, rgba(17,24,39,0.75), rgba(17,24,39,0.15));
          pointer-events: none;
        }
        .heroText {
          position: absolute; left: 18px; bottom: 16px; right: 18px;
          color: white;
        }
        .heroTitle { font-size: 26px; font-weight: 800; letter-spacing: 0.2px; }
        .heroSub { opacity: 0.9; margin-top: 4px; }

        .header {
          display: flex;
          align-items: flex-start;
          justify-content: space-between;
          gap: 12px;
          padding: 18px 20px 8px 20px;
        }
        .title { margin: 0; font-size: 22px; }
        .subtitle { margin: 6px 0 0 0; color: #4b5563; }

        .btnRow { display: flex; gap: 10px; align-items: center; }
        .btn {
          border: none;
          border-radius: 12px;
          padding: 10px 14px;
          cursor: pointer;
          font-weight: 700;
        }
        .btn:hover { opacity: 0.93; }
        .btn:active { transform: translateY(1px); }
        .btn.outline { background: transparent; border: 2px solid; }
        .btn.ghost { background: #f3f4f6; color: #111827; }

        .banner, .status {
          margin: 10px 20px 0 20px;
          padding: 10px 12px;
          border-radius: 12px;
        }
        .banner {
          background: linear-gradient(90deg, #ecfeff, #eef2ff);
          border: 1px solid #a5f3fc;
        }
        .status {
          background: #fef9c3;
          border: 1px solid #fde68a;
        }

        .gridWrap {
          display: flex;
          justify-content: center;
          padding: 20px;
        }
        .grid {
          width: 100%;
          max-width: 860px;
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 12px;
          margin: 0 auto;
        }

        .fieldCard {
          box-sizing: border-box; 
          background: linear-gradient(180deg, #f8fafc, #ffffff);
          border: 1px solid #e5e7eb;
          border-radius: 14px;
          padding: 12px 12px;
          box-shadow: 0 1px 0 rgba(0,0,0,0.02);
          color: #111827;
        }

        .label {
          font-size: 12px;
          font-weight: 800;
          color: #6b7280;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          margin-bottom: 6px;
        }
        .value { font-size: 15px; }

        .input {
          width: 100%;
          box-sizing: border-box;
          border: 1px solid #d1d5db;
          border-radius: 12px;
          padding: 10px 12px;
          font-size: 15px;
          outline: none;
          background: #ffffff;
          color: #111827;
          caret-color: #111827;
        }
        .input:focus {
          border-color: #9ca3af;
          box-shadow: 0 0 0 3px rgba(156,163,175,0.25);
        }

        .footer {
          margin-top: 10px;
          border-top: 1px solid #e5e7eb;
          padding: 12px 20px 16px 20px;
        }
        .muted { color: #6b7280; }

        @media (max-width: 780px) {
          .grid { grid-template-columns: 1fr; }
          .header { flex-direction: column; }
          .hero { height: 180px; }
        }
      `}</style>
    </div>
  );
}

