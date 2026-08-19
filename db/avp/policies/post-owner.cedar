permit(
  principal,
  action in [
    Trailhead::Action::"editPost",
    Trailhead::Action::"deletePost"
  ],
  resource
)
when {
  resource.owner == principal
};
