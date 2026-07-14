import { useNavigate } from "react-router-dom";

export function BackToRisk() {
  const navigate = useNavigate();
  return (
    <button className="btn btn-secondary btn-sm" onClick={() => navigate("/dashboard")}>
      <i aria-hidden="true" className="fa-solid fa-arrow-left" /> Risk Merkezi
    </button>
  );
}
