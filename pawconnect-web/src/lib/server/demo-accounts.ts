import "server-only";

// Must match PawConnect.Api/Infrastructure/Data/Seeding/DemoDataSeeder.cs (or your Seed:DemoPassword secret).
export const DEMO_PASSWORD = "Demo#Paws2026";

export const demoAccounts = [
  { label: "Administrator", email: "demo.admin@example.com", description: "Report queue, listings, users" },
  { label: "Rescuer: Demo Rescue Shelter", email: "demo.rescuer@example.com", description: "Listings, applications to review, an adoption ready to complete" },
  { label: "Rescuer: Street Paws Dhaka", email: "second.rescuer@example.com", description: "Pending applications and an open report" },
  { label: "Adopter: Sam", email: "sam.adopter@example.com", description: "Four applications in different states" },
  { label: "Adopter: Nadia", email: "nadia.adopter@example.com", description: "A completed adoption" },
  { label: "Adopter: Omar", email: "omar.adopter@example.com", description: "An approved foster and a withdrawn application" },
];